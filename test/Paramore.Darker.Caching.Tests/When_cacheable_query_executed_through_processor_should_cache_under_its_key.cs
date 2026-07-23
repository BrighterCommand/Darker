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

namespace Paramore.Darker.Caching.Tests;

/// <summary>
/// End-to-end pipeline tests proving that the runtime <c>query is IAmCacheable</c> detection
/// in <see cref="DefaultCacheKeyGenerator"/> works correctly when a query flows through a real
/// <see cref="QueryProcessor"/> — where the decorator's <c>TQuery</c> is <c>IQuery&lt;TResult&gt;</c>
/// at runtime, not the concrete query type. A directly-instantiated decorator would exercise a
/// resolution path the pipeline never uses (false-green), so these tests drive the full DI stack.
/// </summary>
public class IAmCacheablePipelineTests
{
    [Fact]
    public async Task When_cacheable_query_executed_through_processor_should_cache_under_its_key()
    {
        // Arrange — build a real ServiceProvider with AddDarker + AddCaching + HybridCache.
        // HandlerCallCounter is a singleton so the same instance is injected into every
        // KeyedCacheQueryHandlerAsync and can be resolved for assertions.
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();
        services.AddHybridCache();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(KeyedCacheQueryHandlerAsync).Assembly)
            .AddCaching();

        await using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
        var counter = provider.GetRequiredService<HandlerCallCounter>();

        const string SHARED_KEY = "user-42";

        // Act — first call: IAmCacheable key "user-42", cache miss, handler must run
        var firstQuery = new KeyedCacheQuery { CacheKey = SHARED_KEY, Payload = "alice" };
        var firstResult = await queryProcessor.ExecuteAsync(firstQuery);

        // Assert — handler ran once and result carries the expected payload
        firstResult.ShouldNotBeNull();
        firstResult.Value.ShouldBe("alice");
        counter.CallCount.ShouldBe(1);

        // Act — second call with the same IAmCacheable CacheKey: must be a cache hit
        var secondQuery = new KeyedCacheQuery { CacheKey = SHARED_KEY, Payload = "alice" };
        var secondResult = await queryProcessor.ExecuteAsync(secondQuery);

        // Assert — result served from cache; handler was not re-invoked (count stays at 1)
        secondResult.ShouldNotBeNull();
        secondResult.Value.ShouldBe("alice");
        counter.CallCount.ShouldBe(1,
            "A second call with the same IAmCacheable CacheKey must hit the cache without re-running the handler");

        // Act — third call with a different CacheKey: distinct entry, handler must run again
        const string DIFFERENT_KEY = "user-99";
        var differentQuery = new KeyedCacheQuery { CacheKey = DIFFERENT_KEY, Payload = "bob" };
        var differentResult = await queryProcessor.ExecuteAsync(differentQuery);

        // Assert — handler ran for the new key (counter reaches 2); result carries new payload
        differentResult.ShouldNotBeNull();
        differentResult.Value.ShouldBe("bob");
        counter.CallCount.ShouldBe(2,
            "A call with a different IAmCacheable CacheKey must miss the cache and re-run the handler, proving key drives cache-entry identity");
    }
}
