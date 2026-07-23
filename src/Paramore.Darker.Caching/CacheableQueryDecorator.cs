#region Licence
/* The MIT License (MIT)
Copyright © 2026 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Observability;

namespace Paramore.Darker.Caching;

/// <summary>
/// Synchronous pipeline decorator that caches query results via <see cref="HybridCache"/>.
/// On a cache miss the factory runs, invoking the next handler in the pipeline and
/// storing the result; on a cache hit the factory never runs and the cached value is
/// returned immediately without invoking the handler.
/// </summary>
/// <remarks>
/// <para>
/// The decorator resolves <see cref="HybridCache"/> and <see cref="ICacheKeyGenerator"/>
/// lazily from the <see cref="IServiceProvider"/> so they are not captured at
/// construction time (which occurs per-query).
/// </para>
/// <para>
/// The cache key is derived from the <b>runtime</b> query object rather than from
/// <typeparamref name="TQuery"/>. In the Darker pipeline every decorator is closed over
/// <c>IQuery&lt;TResult&gt;</c> (not the concrete query type), so using
/// <typeparamref name="TQuery"/> would produce wrong keys. Always pass the runtime
/// argument to <see cref="ICacheKeyGenerator.GenerateKey"/>.
/// </para>
/// <para>
/// <strong>Async-first with a sync fast-path.</strong> This synchronous decorator calls the
/// asynchronous <see cref="HybridCache.GetOrCreateAsync{TState,T}"/>, which returns a
/// <see cref="ValueTask{TResult}"/>. On an in-memory L1 cache hit that value task is already
/// completed; we inspect <see cref="ValueTask{TResult}.IsCompletedSuccessfully"/> and return
/// <see cref="ValueTask{TResult}.Result"/> synchronously on the originating thread — no
/// thread-pool hop. When the value task is not yet complete (e.g. a distributed L2 read) we block
/// via <c>.AsTask().GetAwaiter().GetResult()</c>.
/// </para>
/// <para>
/// <strong>Deadlock safety.</strong> Before the cache call the ambient
/// <see cref="System.Threading.SynchronizationContext"/> is suppressed (set to <see langword="null"/>)
/// and restored in a <c>finally</c>. This is the sync-over-async equivalent of
/// <c>ConfigureAwait(false)</c>: the cache's internal continuations capture the null context and
/// resume on a thread-pool thread rather than a single-threaded originating context (classic
/// ASP.NET request thread, WPF/WinForms UI thread), so the blocking wait cannot deadlock. Because
/// <c>Execute</c> is fully synchronous and restores the context before returning, the caller never
/// observes the suppression.
/// </para>
/// </remarks>
/// <typeparam name="TQuery">The query type, constrained to <see cref="IQuery{TResult}"/>.</typeparam>
/// <typeparam name="TResult">The result type produced by the query.</typeparam>
public sealed class CacheableQueryDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private readonly IServiceProvider _serviceProvider;
    private int _expirationSeconds;

    /// <inheritdoc />
    public IQueryContext Context { get; set; } = null!;

    /// <summary>
    /// Initialises the decorator with the DI service provider used to resolve
    /// <see cref="HybridCache"/> and <see cref="ICacheKeyGenerator"/> at execution time.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    public CacheableQueryDecorator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Expects <paramref name="attributeParams"/> to contain a single element: the
    /// expiration in seconds (as an <see cref="int"/>), as returned by
    /// <see cref="CacheableQueryAttribute.GetAttributeParams"/>.
    /// </remarks>
    public void InitializeFromAttributeParams(object[] attributeParams)
    {
        var expirationSeconds = (int)attributeParams[0];
        if (expirationSeconds <= 0)
            throw new ConfigurationException(
                $"[CacheableQueryAttribute] expirationSeconds must be a positive integer; got {expirationSeconds}. " +
                "Specify a value greater than zero so every cached query has an explicit, positive lifetime.");
        _expirationSeconds = expirationSeconds;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Delegates to <see cref="HybridCache.GetOrCreateAsync{TState,T}"/> using the state overload
    /// to avoid capturing <paramref name="next"/> and <paramref name="query"/> in a closure. The
    /// factory wraps the synchronous <paramref name="next"/> delegate in a completed
    /// <see cref="ValueTask{TResult}"/>. A synchronously-completed value task (in-memory L1 hit) is
    /// read directly via <see cref="ValueTask{TResult}.Result"/>; otherwise the value task is
    /// resolved by blocking via <c>.AsTask().GetAwaiter().GetResult()</c>. The ambient
    /// <see cref="System.Threading.SynchronizationContext"/> is suppressed for the duration of the
    /// call and restored in a <c>finally</c> so the blocking wait cannot deadlock under a
    /// single-threaded context.
    /// </remarks>
    public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
    {
        var cache = _serviceProvider.GetService<HybridCache>()
            ?? throw new ConfigurationException(
                "No HybridCache is registered in the DI container. " +
                "Call services.AddHybridCache() (or register a HybridCache implementation such as FusionCache) " +
                "before using [CacheableQueryAttribute].");
        var keyGenerator = _serviceProvider.GetRequiredService<ICacheKeyGenerator>();

        // Use the runtime query argument — never typeof(TQuery) — to compute the key.
        var key = keyGenerator.GenerateKey(query);

        var options = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromSeconds(_expirationSeconds)
        };

        var tags = CacheTagHelper.ReadTags(Context);
        var ran = false;
        var state = (next, query);

        // Suppress any ambient SynchronizationContext for the duration of the cache call. If a
        // single-threaded context (classic ASP.NET request thread, WPF/WinForms UI thread) were
        // captured by the cache's internal awaits, blocking on the result below would deadlock.
        // Removing the context is the sync-over-async equivalent of ConfigureAwait(false): the
        // cache read's continuations resume on a thread-pool thread, not the originating thread.
        // Execute is fully synchronous and restores the context in the finally before returning,
        // so the caller never observes the suppression. The original context is restored on every
        // path, including exceptions.
        var originalContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        TResult result;
        try
        {
            var valueTask = cache.GetOrCreateAsync(
                key,
                state,
                (s, ct) =>
                {
                    ran = true;
                    // Wrap the synchronous next() result in a completed ValueTask so the factory
                    // never blocks the thread pool.
                    return ValueTask.FromResult(s.next(s.query));
                },
                options,
                tags: tags,
                cancellationToken: CancellationToken.None);

            // Fast path: a synchronously-completed ValueTask (in-memory L1 hit) never suspended,
            // so no continuation was scheduled — read the result directly on the originating
            // thread with no thread-pool hop. Consume the ValueTask exactly once.
            if (valueTask.IsCompletedSuccessfully)
                result = valueTask.Result;
            else
                result = valueTask.AsTask().GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }

        var outcome = ran ? CacheOutcome.Miss : CacheOutcome.Hit;
        Context.Span?.SetTag(DarkerSemanticConventions.CacheOutcome, outcome == CacheOutcome.Hit ? "hit" : "miss");
        return result;
    }
}
