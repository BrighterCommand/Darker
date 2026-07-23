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
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Caching.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

/// <summary>
/// End-to-end pipeline test that verifies FR11 negative-caching behaviour:
/// the caching decorator does <em>not</em> special-case a <c>null</c> result.
/// A null returned by the handler on a cache miss is stored as-is by
/// <c>GetOrCreateAsync</c>; a subsequent call with the same key within the
/// expiry window returns the cached <c>null</c> as a hit without re-running the handler.
/// </summary>
public class NullResultCachingTests
{
    [Fact]
    public async Task When_handler_returns_null_should_cache_null_and_serve_hit()
    {
        // Arrange — build a real ServiceProvider with AddDarker + AddCaching + HybridCache.
        // HandlerCallCounter is a singleton so the same instance is injected into
        // NullReturningCacheQueryHandlerAsync and can be resolved here for assertions.
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();
        services.AddHybridCache();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(NullReturningCacheQueryHandlerAsync).Assembly)
            .AddCaching();

        await using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
        var counter = provider.GetRequiredService<HandlerCallCounter>();

        var QUERY = new NullReturningCacheQuery { QueryKey = "null-result-key" };

        // Act — first call: cache miss; the factory runs and the handler returns null
        var firstResult = await queryProcessor.ExecuteAsync(QUERY);

        // Assert — handler ran once and the result is null (the handler always returns null)
        firstResult.ShouldBeNull();
        counter.CallCount.ShouldBe(1, "the handler must run exactly once on a cache miss");

        // Act — second call with the same query: the cached null must be returned as a hit
        var secondResult = await queryProcessor.ExecuteAsync(QUERY);

        // Assert — the cached null is returned and the handler was NOT re-invoked
        secondResult.ShouldBeNull();
        counter.CallCount.ShouldBe(1,
            "the handler must not re-run — the decorator does not special-case null; the cached null is a hit");
    }
}
