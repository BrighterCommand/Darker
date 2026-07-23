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
/// End-to-end pipeline test: proves that the async caching decorator runs the handler exactly
/// once on a cache miss and returns the cached result on a subsequent hit — exercising the real
/// <see cref="QueryProcessor"/> so that the runtime-type trap in
/// <see cref="CacheableQueryDecoratorAsync{TQuery,TResult}"/> is exposed rather than hidden.
/// </summary>
public class CachingQueryProcessorPipelineTests
{
    [Fact]
    public async Task When_cached_query_executed_twice_should_run_handler_once_and_serve_hit()
    {
        // Arrange — build a real ServiceProvider with the full Darker DI pipeline.
        // HandlerCallCounter is a singleton so the same instance is injected into every
        // CacheTestQueryHandlerAsync and can be resolved from the provider for assertions.
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
        var counter = provider.GetRequiredService<HandlerCallCounter>();

        var QUERY = new CacheTestQuery { Payload = "hello" };

        // Act — first call: cache miss, the factory runs and the handler is invoked
        var firstResult = await queryProcessor.ExecuteAsync(QUERY);

        // Assert — handler ran once and the result contains the expected value
        firstResult.ShouldNotBeNull();
        firstResult.Value.ShouldBe("hello");
        counter.CallCount.ShouldBe(1);

        // Act — second call with the same query: cache hit, handler must not run again
        var secondResult = await queryProcessor.ExecuteAsync(QUERY);

        // Assert — same result returned from cache and handler was not re-invoked
        secondResult.ShouldNotBeNull();
        secondResult.Value.ShouldBe("hello");
        counter.CallCount.ShouldBe(1);
    }
}
