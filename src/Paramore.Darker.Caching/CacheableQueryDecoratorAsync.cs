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

namespace Paramore.Darker.Caching;

/// <summary>
/// Async pipeline decorator that caches query results via <see cref="HybridCache"/>.
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
/// </remarks>
/// <typeparam name="TQuery">The query type, constrained to <see cref="IQuery{TResult}"/>.</typeparam>
/// <typeparam name="TResult">The result type produced by the query.</typeparam>
public sealed class CacheableQueryDecoratorAsync<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
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
    public CacheableQueryDecoratorAsync(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Expects <paramref name="attributeParams"/> to contain a single element: the
    /// expiration in seconds (as an <see cref="int"/>), as returned by
    /// <see cref="CacheableQueryAttributeAsync.GetAttributeParams"/>.
    /// </remarks>
    public void InitializeFromAttributeParams(object[] attributeParams)
    {
        var expirationSeconds = (int)attributeParams[0];
        if (expirationSeconds <= 0)
            throw new ConfigurationException(
                $"[CacheableQueryAttributeAsync] expirationSeconds must be a positive integer; got {expirationSeconds}. " +
                "Specify a value greater than zero so every cached query has an explicit, positive lifetime.");
        _expirationSeconds = expirationSeconds;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Delegates to <see cref="HybridCache.GetOrCreateAsync{TState,T}"/> using the
    /// state overload to avoid capturing variables in a closure. On a miss the factory
    /// invokes <paramref name="next"/> and the result is stored; on a hit the factory
    /// is never called and the cached value is returned.
    /// </remarks>
    public async Task<TResult> ExecuteAsync(
        TQuery query,
        Func<TQuery, CancellationToken, Task<TResult>> next,
        Func<TQuery, CancellationToken, Task<TResult>> fallback,
        CancellationToken cancellationToken = default)
    {
        var cache = _serviceProvider.GetRequiredService<HybridCache>();
        var keyGenerator = _serviceProvider.GetRequiredService<ICacheKeyGenerator>();

        // Use the runtime query argument — never typeof(TQuery) — to compute the key.
        var key = keyGenerator.GenerateKey(query);

        var options = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromSeconds(_expirationSeconds)
        };

        var state = (next, query);
        return await cache.GetOrCreateAsync(
            key,
            state,
            async (s, ct) => await s.next(s.query, ct).ConfigureAwait(false),
            options,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
