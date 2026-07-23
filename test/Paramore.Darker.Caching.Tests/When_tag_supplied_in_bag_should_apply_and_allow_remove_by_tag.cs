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
/// End-to-end test proving that a tag placed in <see cref="IQueryContext.Bag"/> under
/// <see cref="CacheableQueryAttribute.CacheTag"/> is applied to the cache entry so that
/// <see cref="HybridCache.RemoveByTagAsync"/> with that tag evicts the entry, causing the
/// next execution to be a miss that re-runs the handler.
/// </summary>
public class TagBasedEvictionTests
{
    [Fact]
    public async Task When_tag_supplied_in_bag_should_apply_and_allow_remove_by_tag()
    {
        // Arrange — real ServiceProvider with the full Darker DI pipeline and a named tag.
        const string TAG = "test-eviction-tag";

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();
        services.AddHybridCache();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(CacheTestQueryHandlerAsync).Assembly)
            .AddCaching();

        await using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
        var hybridCache = provider.GetRequiredService<HybridCache>();
        var counter = provider.GetRequiredService<HandlerCallCounter>();

        var query = new CacheTestQuery { Payload = "tagged-hello" };

        // Act — first call: cache miss; factory runs; handler is invoked
        var firstResult = await queryProcessor.ExecuteAsync(
            query,
            queryContext: MakeContext(TAG));

        // Assert — handler ran once; result echoes the payload
        firstResult.ShouldNotBeNull();
        firstResult.Value.ShouldBe("tagged-hello");
        counter.CallCount.ShouldBe(1);

        // Act — second call with the same query: the entry is a cache hit; handler must not run again
        var secondResult = await queryProcessor.ExecuteAsync(
            query,
            queryContext: MakeContext(TAG));

        // Assert — cached result returned; handler count unchanged at 1
        secondResult.ShouldNotBeNull();
        secondResult.Value.ShouldBe("tagged-hello");
        counter.CallCount.ShouldBe(1);

        // Act — evict the tagged entry then re-execute
        await hybridCache.RemoveByTagAsync(TAG);
        var thirdResult = await queryProcessor.ExecuteAsync(
            query,
            queryContext: MakeContext(TAG));

        // Assert — after tag eviction the entry is gone; cache miss re-runs the handler
        thirdResult.ShouldNotBeNull();
        thirdResult.Value.ShouldBe("tagged-hello");
        counter.CallCount.ShouldBe(2);
    }

    /// <summary>
    /// Creates a fresh <see cref="QueryContext"/> with the given tag in the Bag so that the
    /// caching decorator reads and applies it on each execution.
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
