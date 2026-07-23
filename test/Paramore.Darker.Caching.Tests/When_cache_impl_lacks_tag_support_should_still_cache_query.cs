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

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Caching.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

/// <summary>
/// End-to-end test proving that when a <see cref="HybridCache"/> implementation lacks
/// tag-based eviction support, a tagged cacheable query still executes successfully: the
/// first call populates the cache, the second call is a hit (handler count 1), and no
/// tagging-related failure surfaces — consistent with FR14's best-effort tagging contract.
/// </summary>
public class NoTagSupportCachingTests
{
    [Fact]
    public async Task When_cache_impl_lacks_tag_support_should_still_cache_query()
    {
        // Arrange — real ServiceProvider with the full Darker DI pipeline and a
        // NoTagSupportHybridCache registered instead of the standard AddHybridCache().
        const string TAG = "fr14-best-effort-tag";

        var noTagCache = new NoTagSupportHybridCache();

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();

        // Register the no-tag-support double as the HybridCache implementation.
        // No AddHybridCache() call — this cache silently ignores tags on GetOrCreateAsync
        // and is a no-op on RemoveByTagAsync.
        services.AddSingleton<HybridCache>(noTagCache);

        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(CacheTestQueryHandlerAsync).Assembly)
            .AddCaching();

        await using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
        var counter = provider.GetRequiredService<HandlerCallCounter>();

        var query = new CacheTestQuery { Payload = "no-tag-support-payload" };

        // Act — first call: cache miss; factory runs; handler is invoked once
        var firstResult = await queryProcessor.ExecuteAsync(
            query,
            queryContext: MakeContext(TAG));

        // Assert — handler ran once; result echoes the payload; no exception thrown
        firstResult.ShouldNotBeNull();
        firstResult.Value.ShouldBe("no-tag-support-payload");
        counter.CallCount.ShouldBe(1);

        // Act — second call with the same query: the entry must be a cache hit even though
        // the underlying cache does not support tags (caching still works — FR14)
        var secondResult = await queryProcessor.ExecuteAsync(
            query,
            queryContext: MakeContext(TAG));

        // Assert — result served from cache; handler NOT invoked again
        secondResult.ShouldNotBeNull();
        secondResult.Value.ShouldBe("no-tag-support-payload");
        counter.CallCount.ShouldBe(1, "second call must be a cache hit; handler must not run again");

        // Act — call RemoveByTagAsync directly to confirm it is a no-op on this implementation
        await noTagCache.RemoveByTagAsync(TAG);

        // Act — third call after the attempted (no-op) eviction: still a hit
        var thirdResult = await queryProcessor.ExecuteAsync(
            query,
            queryContext: MakeContext(TAG));

        // Assert — RemoveByTagAsync was a no-op; entry was NOT evicted; still a cache hit
        thirdResult.ShouldNotBeNull();
        thirdResult.Value.ShouldBe("no-tag-support-payload");
        counter.CallCount.ShouldBe(1, "third call must still be a cache hit after no-op tag eviction");
    }

    /// <summary>
    /// Creates a <see cref="QueryContext"/> with the given tag in <see cref="IQueryContext.Bag"/>
    /// so the caching decorator attempts to apply it on each execution.
    /// </summary>
    private static QueryContext MakeContext(string tag) =>
        new()
        {
            Bag = new Dictionary<string, object>
            {
                [CacheableQueryAttribute.CacheTag] = tag
            }
        };
}
