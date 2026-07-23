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

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Caching.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Paramore.Darker.Caching.Tests;

/// <summary>
/// End-to-end pipeline test: proves that the async caching decorator caches through FusionCache
/// when FusionCache is registered as the <c>HybridCache</c> implementation via its DI extension,
/// with no change to the handler or decorator code (FR6, FR7, ADR 0021 — switching is a pure DI choice).
/// </summary>
public class FusionCacheBackingTests
{
    [Fact]
    public async Task When_backing_cache_is_fusioncache_should_cache_via_di_switch()
    {
        // Arrange — build a real ServiceProvider.
        // The ONLY difference from the Microsoft HybridCache test is that we register
        // FusionCache as the HybridCache implementation instead of AddHybridCache().
        // The handler, decorator, and AddCaching() call are completely unchanged.
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();

        // Register FusionCache as the HybridCache implementation — pure DI switch, no Darker code change.
        services.AddFusionCache()
            .AsHybridCache();

        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(CacheTestQueryHandlerAsync).Assembly)
            .AddCaching();

        await using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
        var counter = provider.GetRequiredService<HandlerCallCounter>();

        var query = new CacheTestQuery { Payload = "fusioncache-test" };

        // Act — first call: cache miss, factory runs and the handler is invoked
        var firstResult = await queryProcessor.ExecuteAsync(query);

        // Assert — handler ran once and the result contains the expected value
        firstResult.ShouldNotBeNull();
        firstResult.Value.ShouldBe("fusioncache-test");
        counter.CallCount.ShouldBe(1);

        // Act — second call with the same query: cache hit served by FusionCache
        var secondResult = await queryProcessor.ExecuteAsync(query);

        // Assert — cached result returned and handler was not re-invoked
        secondResult.ShouldNotBeNull();
        secondResult.Value.ShouldBe("fusioncache-test");
        counter.CallCount.ShouldBe(1, "the handler must not run a second time when FusionCache serves a hit");
    }
}
