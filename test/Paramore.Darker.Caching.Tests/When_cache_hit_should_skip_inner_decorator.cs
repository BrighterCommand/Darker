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
/// End-to-end pipeline test proving that a cache <em>hit</em> short-circuits the pipeline so
/// that decorators ordered <em>inside</em> the cache decorator are not invoked.
/// <para>
/// Decorator ordering recap: <c>PipelineBuilder.GetDecoratorsAsync</c> sorts attributes
/// <c>OrderByDescending(step)</c>, so the highest-step attribute sits closest to the handler
/// (innermost) and the lowest-step attribute is added last and becomes outermost. The cache
/// attribute carries step = 1 (outer); <see cref="RecordingDecoratorAttributeAsync"/> carries
/// step = 2 (inner). On a cache hit the cache decorator returns the cached value without
/// invoking <c>next</c>, so the recording decorator and the handler never run.
/// </para>
/// </summary>
public class CacheHitSkipsInnerDecoratorTests
{
    [Fact]
    public async Task When_cache_hit_should_skip_inner_decorator()
    {
        // Arrange — build a real ServiceProvider with AddDarker + AddCaching + HybridCache.
        // InnerInvocationRecorder is a singleton so the same instance is injected into the
        // recording decorator, the handler, and resolved here for assertions.
        // RegisterDecorator registers RecordingDecoratorAsync<,> so the pipeline factory can
        // resolve it when the handler's [RecordingDecoratorAttributeAsync(2)] is processed.
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<InnerInvocationRecorder>();
        services.AddHybridCache();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(OrderingTestQueryHandlerAsync).Assembly)
            .AddCaching()
            .RegisterDecorator(typeof(RecordingDecoratorAsync<,>));

        await using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
        var recorder = provider.GetRequiredService<InnerInvocationRecorder>();

        var QUERY = new OrderingTestQuery { Payload = "ordering-payload" };

        // Act — first execution: cache miss; both the inner recording decorator and the handler run
        var firstResult = await queryProcessor.ExecuteAsync(QUERY);

        // Assert — on miss: inner decorator ran once, handler ran once, result is correct
        firstResult.ShouldNotBeNull();
        firstResult.Value.ShouldBe("ordering-payload");
        recorder.InnerDecoratorCallCount.ShouldBe(1,
            "on a cache miss the inner recording decorator must run exactly once");
        recorder.HandlerCallCount.ShouldBe(1,
            "on a cache miss the handler must run exactly once");

        // Act — second execution with the same query: cache hit; result served from cache
        var secondResult = await queryProcessor.ExecuteAsync(QUERY);

        // Assert — on hit: inner decorator count stays at 1, handler count stays at 1
        // The cache decorator is outermost (step = 1) and short-circuits `next` on a hit,
        // so the recording decorator (step = 2, inner) and the handler are never entered.
        secondResult.ShouldNotBeNull();
        secondResult.Value.ShouldBe("ordering-payload");
        recorder.InnerDecoratorCallCount.ShouldBe(1,
            "on a cache hit the inner recording decorator must not run — the cache short-circuits next");
        recorder.HandlerCallCount.ShouldBe(1,
            "on a cache hit the handler must not run — the cache short-circuits next");
    }
}
