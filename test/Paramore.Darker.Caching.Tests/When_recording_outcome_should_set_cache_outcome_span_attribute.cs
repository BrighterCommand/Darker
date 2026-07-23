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
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Caching.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

/// <summary>
/// End-to-end pipeline test proving that the async caching decorator writes
/// <c>paramore.darker.cache.outcome</c> = <c>"miss"</c> on a first-time (miss) execution and
/// <c>"hit"</c> on a subsequent (hit) execution, using only the BCL
/// <see cref="System.Diagnostics.Activity"/> API — no OpenTelemetry/metrics dependency.
/// Also proves the null-span guard: when no tracer is configured (<see cref="IQueryContext.Span"/>
/// is null) the same executions succeed without throwing.
/// </summary>
/// <remarks>
/// Uses <see cref="DarkerActivitySourceCollection"/> to prevent parallel execution with other
/// ActivityListener tests so that a leaked listener cannot corrupt the completed-span list.
/// End-to-end through a real <see cref="QueryProcessor"/> — directly instantiating the decorator
/// would bypass the pipeline that populates <see cref="IQueryContext.Span"/> and would be a false
/// test (ADR 0021, Implementation Approach step 9).
/// </remarks>
[Collection("DarkerActivitySource")]
public class CacheOutcomeSpanAttributeTests
{
    private static ActivityListener CreateListener(List<Activity> completed)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == DarkerSemanticConventions.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => completed.Add(a),
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Fact]
    public async Task When_recording_outcome_should_set_cache_outcome_span_attribute()
    {
        // ── Part 1: with tracer — miss sets "miss", subsequent hit sets "hit" ────────────────

        // Arrange — real QueryProcessor with DarkerTracer so Context.Span is populated.
        // The ActivityListener captures spans to the completed list when they stop, so the
        // cache-outcome tag (set during ExecuteAsync) is readable after each call returns.
        var completed = new List<Activity>();
        using var tracer = new DarkerTracer();
        using var listener = CreateListener(completed);

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();
        services.AddSingleton<IAmADarkerTracer>(tracer);
        services.AddHybridCache();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(CacheTestQueryHandlerAsync).Assembly)
            .AddCaching();

        await using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

        var QUERY = new CacheTestQuery { Payload = "outcome-test" };

        // Act — first execution: cache miss; the factory runs and the handler is invoked
        await queryProcessor.ExecuteAsync(QUERY);

        // Assert — miss span carries "miss" on the cache-outcome tag
        completed.Count.ShouldBe(1, "one span should be completed after the miss");
        var missSpan = completed[0];
        missSpan.GetTagItem(DarkerSemanticConventions.CacheOutcome).ShouldBe("miss",
            "a cache miss must leave paramore.darker.cache.outcome = \"miss\" on the query span");

        // Act — second execution with same query: cache hit; the factory does not run
        await queryProcessor.ExecuteAsync(QUERY);

        // Assert — hit span carries "hit" on the cache-outcome tag
        completed.Count.ShouldBe(2, "a second span should be completed after the hit");
        var hitSpan = completed[1];
        hitSpan.GetTagItem(DarkerSemanticConventions.CacheOutcome).ShouldBe("hit",
            "a cache hit must leave paramore.darker.cache.outcome = \"hit\" on the query span");

        // ── Part 2: no-tracer guard — Context.Span is null, the ?. guard must prevent a throw ─

        // Arrange — separate provider with no IAmADarkerTracer so Context.Span is null in the decorator
        var noTracerServices = new ServiceCollection();
        noTracerServices.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        noTracerServices.AddSingleton<HandlerCallCounter>();
        noTracerServices.AddHybridCache();
        noTracerServices
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(CacheTestQueryHandlerAsync).Assembly)
            .AddCaching();

        await using var noTracerProvider = noTracerServices.BuildServiceProvider();
        var noTracerProcessor = noTracerProvider.GetRequiredService<IQueryProcessor>();

        var noTracerQuery = new CacheTestQuery { Payload = "no-tracer" };

        // Act & Assert — miss call (Context.Span is null): must not throw
        var noTracerMissResult = await noTracerProcessor.ExecuteAsync(noTracerQuery);
        noTracerMissResult.ShouldNotBeNull(
            "a cache miss without a tracer must still return a result and not throw");

        // Act & Assert — hit call (Context.Span is null): must not throw
        var noTracerHitResult = await noTracerProcessor.ExecuteAsync(noTracerQuery);
        noTracerHitResult.ShouldNotBeNull(
            "a cache hit without a tracer must still return a result and not throw");
    }
}
