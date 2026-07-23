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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Caching.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

/// <summary>
/// End-to-end pipeline test: proves that after the configured cache TTL elapses, a
/// subsequent query execution re-runs the handler rather than returning a stale cached
/// result — verifying that <see cref="CacheableQueryDecoratorAsync{TQuery,TResult}"/>
/// maps <c>expirationSeconds</c> to <see cref="Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions.Expiration"/>
/// and passes it to every <c>GetOrCreateAsync</c> call.
/// </summary>
public class CacheExpiryTests
{
    [Fact]
    public async Task When_cache_entry_expires_should_rerun_handler()
    {
        // Arrange — build a real ServiceProvider with the full Darker DI pipeline.
        // The handler uses [CacheableQueryAttributeAsync(1, expirationSeconds: 1)] so the
        // cache entry expires after 1 second.
        // HandlerCallCounter is a singleton so the same instance is injected into every
        // handler invocation and can be resolved for assertions.
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();
        services.AddHybridCache();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(ShortExpiryCacheTestQueryHandlerAsync).Assembly)
            .AddCaching();

        await using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
        var counter = provider.GetRequiredService<HandlerCallCounter>();

        var QUERY = new ShortExpiryCacheTestQuery { Payload = "expiry-test" };

        // Act — first call: cache miss, the factory runs and the handler is invoked
        var firstResult = await queryProcessor.ExecuteAsync(QUERY);

        // Assert — handler ran once and the result contains the expected value
        firstResult.ShouldNotBeNull();
        firstResult.Value.ShouldBe("expiry-test");
        counter.CallCount.ShouldBe(1);

        // Act — immediate second call with the same query: cache hit, handler must not run again
        var secondResult = await queryProcessor.ExecuteAsync(QUERY);

        // Assert — same result returned from cache and handler was not re-invoked
        secondResult.ShouldNotBeNull();
        secondResult.Value.ShouldBe("expiry-test");
        counter.CallCount.ShouldBe(1);

        // Arrange — wait slightly longer than the 1-second TTL so the entry expires
        Thread.Sleep(1500);

        // Act — third call after expiry: cache miss, handler must re-run
        var thirdResult = await queryProcessor.ExecuteAsync(QUERY);

        // Assert — handler ran a second time and the result is still correct
        thirdResult.ShouldNotBeNull();
        thirdResult.Value.ShouldBe("expiry-test");
        counter.CallCount.ShouldBe(2);
    }
}
