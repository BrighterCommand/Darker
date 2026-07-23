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

namespace Paramore.Darker.Caching;

/// <summary>
/// Marks a handler's async execute method for caching. When present,
/// <see cref="GetDecoratorType"/> returns <see cref="CacheableQueryDecoratorAsync{TQuery,TResult}"/>
/// so the pipeline resolves and wires the caching decorator automatically.
/// </summary>
/// <remarks>
/// <para>
/// Place this attribute on the handler's <c>ExecuteAsync</c> method and supply a
/// <paramref name="step"/> to control where caching runs in the decorator pipeline (higher step
/// executes first) and an <paramref name="expirationSeconds"/> to set the cache entry lifetime.
/// </para>
/// <para>
/// <strong>Ordering matters — cache hits short-circuit the pipeline.</strong>
/// </para>
/// <para>
/// Darker's decorator pipeline runs decorators in descending step order: the decorator with the
/// highest step executes first (outermost), and the decorator with the lowest step executes last
/// (innermost, just before the handler). On a cache <em>hit</em>, the caching decorator returns
/// the stored result immediately without calling <c>next</c>. Every decorator ordered <em>inside</em>
/// the cache decorator — that is, every decorator whose <paramref name="step"/> is <em>lower</em>
/// than this attribute's <paramref name="step"/> — is therefore <strong>skipped entirely on a
/// hit</strong>.
/// </para>
/// <para>
/// This is a footgun if you rely on inner decorators running for every request. Use the following
/// guidance when choosing a <paramref name="step"/>:
/// <list type="bullet">
///   <item><description>
///     To run logging, retry, or fallback on <strong>every</strong> request (hit or miss), give
///     those decorators a <em>higher</em> step than the cache attribute so they execute
///     <em>outside</em> the cache decorator.
///   </description></item>
///   <item><description>
///     To run logging, retry, or fallback <strong>only on a miss</strong> (i.e. only when the
///     handler actually executes), give those decorators a <em>lower</em> step so they execute
///     <em>inside</em> the cache decorator and are therefore skipped on a hit.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// Example — cache outer, logging inner (logging is skipped on a hit):
/// <code>
/// [CacheableQueryAsync(step: 2, expirationSeconds: 300)]  // outer — executes first
/// [QueryLoggingAsync(1)]                                    // inner — skipped on a cache hit
/// public override Task&lt;MyResult&gt; ExecuteAsync(MyQuery query, CancellationToken ct = default)
/// </code>
/// </para>
/// <para>
/// Example — logging outer, cache inner (logging always runs, even on a hit):
/// <code>
/// [QueryLoggingAsync(2)]                                    // outer — always runs
/// [CacheableQueryAsync(step: 1, expirationSeconds: 300)]  // inner — short-circuits on a hit
/// public override Task&lt;MyResult&gt; ExecuteAsync(MyQuery query, CancellationToken ct = default)
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CacheableQueryAttributeAsync : QueryHandlerAttributeAsync
{
    private readonly int _expirationSeconds;

    /// <summary>
    /// Initialises a new instance of <see cref="CacheableQueryAttributeAsync"/>.
    /// </summary>
    /// <param name="step">
    /// The step order for this decorator in the handler pipeline. Higher values execute earlier.
    /// </param>
    /// <param name="expirationSeconds">
    /// The number of seconds the cached result should be retained.
    /// </param>
    public CacheableQueryAttributeAsync(int step, int expirationSeconds) : base(step)
    {
        _expirationSeconds = expirationSeconds;
    }

    /// <inheritdoc />
    /// <returns>The open-generic <see cref="CacheableQueryDecoratorAsync{TQuery,TResult}"/> type.</returns>
    public override Type GetDecoratorType() => typeof(CacheableQueryDecoratorAsync<,>);

    /// <inheritdoc />
    /// <returns>An array containing the expiration in seconds so the decorator can build its cache options.</returns>
    public override object[] GetAttributeParams() => new object[] { _expirationSeconds };
}
