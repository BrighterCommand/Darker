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

namespace Paramore.Darker.Caching.Tests.TestDoubles;

/// <summary>
/// A test-double synchronous handler for <see cref="SyncCacheTestQuery"/> whose <c>Execute</c>
/// method carries <see cref="CacheableQueryAttribute"/> so that the sync caching decorator
/// is wired into the pipeline automatically.
/// </summary>
/// <remarks>
/// Must be <c>public</c> so that <c>AddHandlersFromAssemblies</c> discovers it via
/// <c>Assembly.GetExportedTypes()</c> when scanning the test assembly in end-to-end tests.
/// The injected <see cref="HandlerCallCounter"/> lets tests assert the exact number of handler
/// invocations without fragile static state.
/// </remarks>
public sealed class SyncCacheTestQueryHandler : QueryHandler<SyncCacheTestQuery, SyncCacheTestQuery.Result>
{
    private readonly HandlerCallCounter _counter;

    /// <summary>
    /// Initialises the handler with the shared call counter.
    /// </summary>
    /// <param name="counter">The counter that tracks how many times the handler body was entered.</param>
    public SyncCacheTestQueryHandler(HandlerCallCounter counter)
    {
        _counter = counter;
    }

    /// <inheritdoc />
    [CacheableQuery(1, 60)]
    public override SyncCacheTestQuery.Result Execute(SyncCacheTestQuery query)
    {
        _counter.Increment();
        return new SyncCacheTestQuery.Result { Value = query.Payload };
    }
}
