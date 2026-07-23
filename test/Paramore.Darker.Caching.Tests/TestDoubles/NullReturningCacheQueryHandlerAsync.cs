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
/// A test-double async handler for <see cref="NullReturningCacheQuery"/> that always returns
/// <c>null</c>, exercising the decorator's negative-caching behaviour (FR11).
/// </summary>
/// <remarks>
/// The injected <see cref="HandlerCallCounter"/> lets end-to-end tests assert that the
/// handler runs exactly once: a second call must serve the cached <c>null</c> as a hit
/// and must not re-invoke the handler.
/// </remarks>
public sealed class NullReturningCacheQueryHandlerAsync
    : QueryHandlerAsync<NullReturningCacheQuery, NullReturningCacheQuery.Result>
{
    private readonly HandlerCallCounter _counter;

    /// <summary>
    /// Initialises the handler with the shared call counter.
    /// </summary>
    /// <param name="counter">Tracks how many times the handler body was entered.</param>
    public NullReturningCacheQueryHandlerAsync(HandlerCallCounter counter)
    {
        _counter = counter;
    }

    /// <inheritdoc />
    [CacheableQueryAttributeAsync(1, 60)]
    public override Task<NullReturningCacheQuery.Result> ExecuteAsync(
        NullReturningCacheQuery query,
        CancellationToken cancellationToken = default)
    {
        _counter.Increment();
        return Task.FromResult(default(NullReturningCacheQuery.Result));
    }
}
