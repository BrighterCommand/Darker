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

using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Caching.Tests.TestDoubles;

/// <summary>
/// A test-double async handler for <see cref="KeyedCacheQuery"/> whose <c>ExecuteAsync</c>
/// carries <see cref="CacheableQueryAttributeAsync"/> so that the caching decorator is wired
/// into the pipeline automatically when the test registers via
/// <c>AddHandlersFromAssemblies</c>.
/// </summary>
/// <remarks>
/// The injected <see cref="HandlerCallCounter"/> lets end-to-end tests assert the exact number
/// of handler invocations. Because the pipeline closes decorators over <c>IQuery&lt;TResult&gt;</c>
/// (not the concrete type), this handler exercises the runtime <c>query is IAmCacheable</c>
/// detection path in <see cref="DefaultCacheKeyGenerator"/> through a real
/// <see cref="QueryProcessor"/>.
/// </remarks>
public sealed class KeyedCacheQueryHandlerAsync : QueryHandlerAsync<KeyedCacheQuery, KeyedCacheQuery.Result>
{
    private readonly HandlerCallCounter _counter;

    /// <summary>
    /// Initialises the handler with the shared call counter.
    /// </summary>
    /// <param name="counter">Tracks how many times the handler body was entered.</param>
    public KeyedCacheQueryHandlerAsync(HandlerCallCounter counter)
    {
        _counter = counter;
    }

    /// <inheritdoc />
    [CacheableQueryAttributeAsync(1, 60)]
    public override Task<KeyedCacheQuery.Result> ExecuteAsync(
        KeyedCacheQuery query,
        CancellationToken cancellationToken = default)
    {
        _counter.Increment();
        return Task.FromResult(new KeyedCacheQuery.Result { Value = query.Payload });
    }
}
