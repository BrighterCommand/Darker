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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Caching.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

/// <summary>
/// End-to-end sync pipeline test: proves that the sync caching decorator serves a warm cache
/// entry via the <c>IsCompletedSuccessfully</c> fast path on a <see cref="System.Threading.Tasks.ValueTask{T}"/>
/// returned by HybridCache, returning the cached result without re-running the handler.
/// Also verifies the shared <see cref="CacheableQueryAttribute.CacheTag"/> constant value.
/// </summary>
public class SyncCacheValueTaskFastPathTests
{
    [Fact]
    public void When_sync_cache_value_task_completed_should_return_synchronously()
    {
        // Arrange — build a real ServiceProvider with the full Darker DI pipeline (sync path).
        // HandlerCallCounter is a singleton so the same instance is injected into every
        // SyncCacheTestQueryHandler and can be resolved from the provider for assertions.
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();
        services.AddHybridCache();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(SyncCacheTestQueryHandler).Assembly)
            .AddCaching();

        using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
        var counter = provider.GetRequiredService<HandlerCallCounter>();

        var QUERY = new SyncCacheTestQuery { Payload = "sync-hello" };

        // Act — first call: cache miss, the factory runs and the handler is invoked
        var firstResult = queryProcessor.Execute(QUERY);

        // Assert — handler ran once and the result contains the expected value
        firstResult.ShouldNotBeNull();
        firstResult.Value.ShouldBe("sync-hello");
        counter.CallCount.ShouldBe(1);

        // Act — second call with the same query: cache hit, handler must not run again.
        // The HybridCache in-memory L1 hit returns a completed ValueTask synchronously,
        // so the sync decorator's IsCompletedSuccessfully fast path is taken.
        var secondResult = queryProcessor.Execute(QUERY);

        // Assert — same result returned from cache and handler was not re-invoked
        secondResult.ShouldNotBeNull();
        secondResult.Value.ShouldBe("sync-hello");
        counter.CallCount.ShouldBe(1);

        // Assert — shared CacheTag constant is the canonical tag value
        CacheableQueryAttribute.CacheTag.ShouldBe("Paramore.Darker.Caching.Tag");
    }
}
