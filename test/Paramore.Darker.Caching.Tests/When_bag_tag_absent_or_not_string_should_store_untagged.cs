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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Caching.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

/// <summary>
/// End-to-end tests proving that when the <see cref="CacheableQueryAttribute.CacheTag"/> Bag key
/// is absent, or its value is not a non-empty string (e.g. <see langword="null"/>, empty,
/// whitespace, or a non-string object), the cache entry is stored untagged and no exception is
/// thrown — consistent with the best-effort stance of FR9.
/// </summary>
public class UntaggedStorageTests
{
    [Fact]
    public async Task When_bag_tag_absent_or_not_string_should_store_untagged()
    {
        // ─────────────────────────────────────────────────────────────────
        // Scenario 1 — CacheTag key is absent from the Bag entirely
        // ─────────────────────────────────────────────────────────────────

        // Arrange — no CacheTag entry; Bag is left empty (the default)
        var (processor1, counter1, provider1) = BuildProcessor();
        await using (provider1)
        {
            var query1 = new CacheTestQuery { Payload = "absent-tag-scenario" };
            var context1 = new QueryContext(); // Bag is an empty dictionary by default

            // Act — first call: cache miss; factory runs; handler is invoked
            var result1 = await processor1.ExecuteAsync(query1, queryContext: context1);

            // Act — second call with the same query: entry is stored untagged; should be a cache hit
            var result2 = await processor1.ExecuteAsync(query1, queryContext: context1);

            // Assert — no exception thrown; caching works; handler ran exactly once
            result1.ShouldNotBeNull();
            result2.ShouldNotBeNull();
            result2.Value.ShouldBe(result1.Value);
            counter1.CallCount.ShouldBe(1, "second call should be a cache hit; handler must not run again");
        }

        // ─────────────────────────────────────────────────────────────────
        // Scenario 2 — CacheTag value is a non-string object (e.g. int 42)
        // ─────────────────────────────────────────────────────────────────

        // Arrange — CacheTag key present but value is int, not string
        var (processor2, counter2, provider2) = BuildProcessor();
        await using (provider2)
        {
            var query2 = new CacheTestQuery { Payload = "non-string-tag-scenario" };
            var context2 = new QueryContext
            {
                Bag = new Dictionary<string, object>
                {
                    [CacheableQueryAttribute.CacheTag] = 42 // int — not a string; must be ignored
                }
            };

            // Act — first call: cache miss; factory runs; entry stored untagged (no throw)
            var result3 = await processor2.ExecuteAsync(query2, queryContext: context2);

            // Act — second call with the same query: untagged entry is still a cache hit
            var result4 = await processor2.ExecuteAsync(query2, queryContext: context2);

            // Assert — no exception thrown; caching works; handler ran exactly once
            result3.ShouldNotBeNull();
            result4.ShouldNotBeNull();
            result4.Value.ShouldBe(result3.Value);
            counter2.CallCount.ShouldBe(1, "second call should be a cache hit; handler must not run again");
        }

        // ─────────────────────────────────────────────────────────────────
        // Scenario 3 — CacheTag value is a whitespace-only string
        // ─────────────────────────────────────────────────────────────────

        // Arrange — CacheTag key present but value is whitespace; treated as absent
        var (processor3, counter3, provider3) = BuildProcessor();
        await using (provider3)
        {
            var query3 = new CacheTestQuery { Payload = "whitespace-tag-scenario" };
            var context3 = new QueryContext
            {
                Bag = new Dictionary<string, object>
                {
                    [CacheableQueryAttribute.CacheTag] = "   " // whitespace-only — must be treated as absent
                }
            };

            // Act — first call: cache miss; factory runs; entry stored untagged (no throw)
            var result5 = await processor3.ExecuteAsync(query3, queryContext: context3);

            // Act — second call with the same query: untagged entry is still a cache hit
            var result6 = await processor3.ExecuteAsync(query3, queryContext: context3);

            // Assert — no exception thrown; caching works; handler ran exactly once
            result5.ShouldNotBeNull();
            result6.ShouldNotBeNull();
            result6.Value.ShouldBe(result5.Value);
            counter3.CallCount.ShouldBe(1, "second call should be a cache hit; handler must not run again");
        }
    }

    /// <summary>
    /// Builds a fresh DI container with the full Darker/caching pipeline and returns the
    /// processor, call counter, and provider for one isolated scenario.
    /// </summary>
    private static (IQueryProcessor processor, HandlerCallCounter counter, ServiceProvider provider) BuildProcessor()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();
        services.AddHybridCache();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(CacheTestQueryHandlerAsync).Assembly)
            .AddCaching();

        var provider = services.BuildServiceProvider();
        return (
            provider.GetRequiredService<IQueryProcessor>(),
            provider.GetRequiredService<HandlerCallCounter>(),
            provider
        );
    }
}
