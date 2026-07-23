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
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Paramore.Darker.Caching;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Diagnostics.Tests;

/// <summary>
/// End-to-end verification (ADR 0021 §Implementation Approach step 8) that executing a cacheable
/// query twice through a real <see cref="QueryProcessor"/> — with tracing and
/// <see cref="DarkerMetricsBuilderExtensions.AddDarkerInstrumentation"/> both wired — records
/// exactly one <c>paramore.darker.cache.outcome = "miss"</c> and one
/// <c>paramore.darker.cache.outcome = "hit"</c> measurement on
/// <c>paramore.darker.cache.requests</c>.
/// When the <c>emitCacheMetrics</c> toggle is disabled, no counter is recorded even though
/// results are unaffected (FR10, Resolved Decision 4).
/// </summary>
/// <remarks>
/// Tests are kept single-threaded / sequential so the HybridCache stampede-protection caveat
/// (joined concurrent callers observe <c>ran == false</c> and are counted as hits) cannot skew
/// assertions — the caveat is documented in ADR 0021 Risks and accepted as a metrics-only
/// inaccuracy; it does not affect returned results.
/// </remarks>
[Collection("DarkerMeter")]
public class CachedQueryMetricsEmissionTests
{
    // ── Test doubles ──────────────────────────────────────────────────────────────────────────

    private sealed class CacheMetricsQuery : IQuery<string>
    {
        public string Payload { get; init; } = "test-payload";
    }

    /// <summary>Thread-safe counter injected into the handler to count invocations.</summary>
    private sealed class CacheMetricsCallCounter
    {
        public int CallCount { get; private set; }
        public void Increment() => CallCount++;
    }

    /// <summary>
    /// Async handler decorated with <see cref="CacheableQueryAttributeAsync"/> so the caching
    /// decorator is wired into the pipeline. Increments the shared counter on every handler body
    /// entry so tests can assert the exact number of handler runs.
    /// </summary>
    private sealed class CacheMetricsHandlerAsync : QueryHandlerAsync<CacheMetricsQuery, string>
    {
        private readonly CacheMetricsCallCounter _counter;

        public CacheMetricsHandlerAsync(CacheMetricsCallCounter counter)
        {
            _counter = counter;
        }

        [CacheableQueryAttributeAsync(1, 60)]
        public override Task<string> ExecuteAsync(
            CacheMetricsQuery query,
            CancellationToken cancellationToken = default)
        {
            _counter.Increment();
            return Task.FromResult(query.Payload);
        }
    }

    /// <summary>
    /// Minimal <see cref="IMeterFactory"/> that creates bare <see cref="Meter"/> instances.
    /// In production the Generic Host supplies this. This stub fulfils that role in a bare
    /// <see cref="ServiceCollection"/> so <see cref="CacheMeter"/> can be activated from DI.
    /// </summary>
    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options.Name, options.Version);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters)
                meter.Dispose();
        }
    }

    // ── Service provider builder ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a fully-wired <see cref="ServiceProvider"/> containing: the OpenTelemetry tracer
    /// (so query spans are created and the <c>cache.outcome</c> attribute is set), the meter
    /// pipeline (so the span attribute drives a counter), HybridCache, and the Darker handler
    /// pipeline with the caching decorator.
    /// </summary>
    /// <param name="metrics">Collector populated by <c>AddInMemoryExporter</c>.</param>
    /// <param name="counter">Counter shared with the handler for invocation assertions.</param>
    /// <param name="emitCacheMetrics">
    /// Mirrors the <c>AddDarkerInstrumentation(emitCacheMetrics)</c> toggle (FR10).
    /// </param>
    private static ServiceProvider BuildStack(
        List<Metric> metrics,
        CacheMetricsCallCounter counter,
        bool emitCacheMetrics)
    {
        var services = new ServiceCollection();

        // IMeterFactory is provided by the Generic Host in production. Stub it here.
        services.TryAddSingleton<IMeterFactory, TestMeterFactory>();

        // HybridCache needs logging; supply a null-sink factory.
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));

        // WithMetrics BEFORE WithTracing: the meter registrations (IAmADarkerQueryMeter etc.)
        // must be visible when the tracer builder's AddDarkerInstrumentation checks for them
        // (ConfigureServices callbacks run immediately at call time, not lazily at build time).
        services.AddOpenTelemetry()
            .WithMetrics(b => b
                .AddDarkerInstrumentation(emitCacheMetrics)
                .AddInMemoryExporter(metrics))
            .WithTracing(b => b.AddDarkerInstrumentation());

        // Register HybridCache (in-memory, default Microsoft implementation).
        services.AddHybridCache();

        // Register the shared call counter before building so it can be injected into the handler.
        services.AddSingleton(counter);

        // Wire Darker: register the handler manually (avoids assembly scanning the entire test
        // project) and add the caching decorator + default cache-key generator.
        services
            .AddDarker()
            .AddAsyncHandlers(registry =>
                registry.Register<CacheMetricsQuery, string, CacheMetricsHandlerAsync>())
            .AddCaching();

        return services.BuildServiceProvider();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task When_cached_query_executed_with_metrics_should_emit_hit_and_miss_counters()
    {
        // Arrange
        var metrics = new List<Metric>();
        var counter = new CacheMetricsCallCounter();
        await using var sp = BuildStack(metrics, counter, emitCacheMetrics: true);

        var tracerProvider = sp.GetRequiredService<TracerProvider>();
        var meterProvider = sp.GetRequiredService<MeterProvider>();
        var queryProcessor = sp.GetRequiredService<IQueryProcessor>();

        var query = new CacheMetricsQuery { Payload = "hello" };

        // Act — first execution: cache miss — the factory runs, handler invoked, result cached
        var firstResult = await queryProcessor.ExecuteAsync(query);

        // Act — second execution: cache hit — factory does NOT run, handler NOT invoked
        var secondResult = await queryProcessor.ExecuteAsync(query);

        // Flush spans through DarkerMetricsFromTracesProcessor and then flush the counter
        tracerProvider.ForceFlush();
        meterProvider.ForceFlush();

        // Assert — results are correct
        firstResult.ShouldBe("hello");
        secondResult.ShouldBe("hello");
        counter.CallCount.ShouldBe(1); // handler ran exactly once (on the miss)

        // Assert — paramore.darker.cache.requests has exactly one miss point and one hit point
        var cacheMetric = metrics.Single(m => m.Name == DarkerSemanticConventions.CacheRequestsMetricName);

        var missSum = 0L;
        var hitSum = 0L;

        foreach (var point in cacheMetric.GetMetricPoints())
        {
            foreach (var tag in point.Tags)
            {
                if (tag.Key == DarkerSemanticConventions.CacheOutcome)
                {
                    if (tag.Value is "miss") missSum += point.GetSumLong();
                    else if (tag.Value is "hit") hitSum += point.GetSumLong();
                }
            }
        }

        missSum.ShouldBe(1L, "first execution should record exactly one miss");
        hitSum.ShouldBe(1L, "second execution should record exactly one hit");
    }

    [Fact]
    public async Task When_cached_query_executed_with_cache_metrics_disabled_should_record_no_cache_counters()
    {
        // Arrange — same wiring but with the cache-metrics opt-out toggle engaged
        var metrics = new List<Metric>();
        var counter = new CacheMetricsCallCounter();
        await using var sp = BuildStack(metrics, counter, emitCacheMetrics: false);

        var tracerProvider = sp.GetRequiredService<TracerProvider>();
        var meterProvider = sp.GetRequiredService<MeterProvider>();
        var queryProcessor = sp.GetRequiredService<IQueryProcessor>();

        var query = new CacheMetricsQuery { Payload = "world" };

        // Act — first execution: cache miss
        var firstResult = await queryProcessor.ExecuteAsync(query);

        // Act — second execution: cache hit
        var secondResult = await queryProcessor.ExecuteAsync(query);

        tracerProvider.ForceFlush();
        meterProvider.ForceFlush();

        // Assert — query results are still correct even though metrics are suppressed
        firstResult.ShouldBe("world");
        secondResult.ShouldBe("world");
        counter.CallCount.ShouldBe(1); // handler still ran exactly once (on the miss)

        // Assert — no measurements on paramore.darker.cache.requests (toggle disabled)
        metrics.ShouldNotContain(m => m.Name == DarkerSemanticConventions.CacheRequestsMetricName);
    }
}
