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
/// A test-double async handler for <see cref="ShortExpiryCacheTestQuery"/> whose
/// <c>ExecuteAsync</c> carries a 1-second <see cref="CacheableQueryAttributeAsync"/> so
/// that expiry tests can observe handler re-execution after the TTL elapses.
/// </summary>
/// <remarks>
/// Must be <c>public</c> so that <c>AddHandlersFromAssemblies</c> discovers it via
/// <c>Assembly.GetExportedTypes()</c> when scanning the test assembly in end-to-end tests.
/// </remarks>
public sealed class ShortExpiryCacheTestQueryHandlerAsync
    : QueryHandlerAsync<ShortExpiryCacheTestQuery, ShortExpiryCacheTestQuery.Result>
{
    private readonly HandlerCallCounter _counter;

    /// <summary>
    /// Initialises the handler with the shared call counter.
    /// </summary>
    /// <param name="counter">The counter that tracks how many times the handler body was entered.</param>
    public ShortExpiryCacheTestQueryHandlerAsync(HandlerCallCounter counter)
    {
        _counter = counter;
    }

    /// <inheritdoc />
    [CacheableQueryAttributeAsync(1, 1)]
    public override Task<ShortExpiryCacheTestQuery.Result> ExecuteAsync(
        ShortExpiryCacheTestQuery query,
        CancellationToken cancellationToken = default)
    {
        _counter.Increment();
        return Task.FromResult(new ShortExpiryCacheTestQuery.Result { Value = query.Payload });
    }
}
