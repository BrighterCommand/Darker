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

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Caching.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

/// <summary>
/// End-to-end sync pipeline test: proves the blocking fallback branch in
/// <c>CacheableQueryDecorator.Execute</c> — the <c>else</c> path that calls
/// <c>.AsTask().GetAwaiter().GetResult()</c> — when
/// <see cref="HybridCache.GetOrCreateAsync{TState,T}"/> returns a
/// <see cref="System.Threading.Tasks.ValueTask{T}"/> that is not already completed.
/// </summary>
/// <remarks>
/// Uses <see cref="AsyncOnMissHybridCache"/> (registered in place of the real
/// <see cref="HybridCache"/>) which always yields before calling the factory so the
/// returned <see cref="System.Threading.Tasks.ValueTask{T}"/> is never
/// <c>IsCompletedSuccessfully</c> at the call site.
/// </remarks>
public class SyncCacheValueTaskBlockingFallbackTests
{
    [Fact]
    public void When_sync_cache_value_task_not_completed_should_block_and_return()
    {
        // Arrange — register AsyncOnMissHybridCache instead of AddHybridCache() so that
        // GetOrCreateAsync always yields before invoking the factory.  This guarantees the
        // returned ValueTask is NOT IsCompletedSuccessfully, forcing the else blocking-fallback
        // branch in CacheableQueryDecorator.Execute.
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();
        services.AddSingleton<HybridCache, AsyncOnMissHybridCache>();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(SyncCacheTestQueryHandler).Assembly)
            .AddCaching();

        using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
        var counter = provider.GetRequiredService<HandlerCallCounter>();

        var QUERY = new SyncCacheTestQuery { Payload = "async-miss-payload" };

        // Act — Execute uses the sync pipeline; the decorator receives a non-completed ValueTask
        // from AsyncOnMissHybridCache and blocks via .AsTask().GetAwaiter().GetResult().
        var result = queryProcessor.Execute(QUERY);

        // Assert — the correct result is returned via the blocking fallback path
        result.ShouldNotBeNull();
        result.Value.ShouldBe("async-miss-payload");

        // Assert — the handler ran exactly once; the ValueTask was consumed once (not twice)
        counter.CallCount.ShouldBe(1);
    }
}
