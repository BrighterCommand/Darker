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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Paramore.Darker.Extensions.Diagnostics.Observability;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Diagnostics.Tests;

[Collection("DarkerMeter")]
public class CacheMetricsFromTracesProcessorTests : IDisposable
{
    private readonly List<Metric> _metrics;
    private readonly MeterProvider _meterProvider;
    private readonly QueryMeter _queryMeter;
    private readonly DbMeter _dbMeter;
    private readonly CacheMeter _cacheMeter;
    private readonly IAmADarkerTracer _tracer;
    private readonly DarkerMetricsFromTracesProcessor _darkerMetricsFromTracesProcessor;
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;
    private readonly SimpleMeterFactory _meterFactory;

    private sealed class SimpleMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = new();

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

    public CacheMetricsFromTracesProcessorTests()
    {
        _metrics = new List<Metric>();

        // Build the MeterProvider BEFORE creating the meters so that the SDK's
        // MeterListener is registered and will pick up the counters when they are published.
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(DarkerSemanticConventions.MeterName)
            .AddInMemoryExporter(_metrics)
            .Build()!;

        _meterFactory = new SimpleMeterFactory();
        _queryMeter = new QueryMeter(_meterFactory, _meterProvider);
        _dbMeter = new DbMeter(_meterFactory, _meterProvider);
        _cacheMeter = new CacheMeter(_meterFactory, _meterProvider);

        _tracer = new DarkerTracer();

        _darkerMetricsFromTracesProcessor = new DarkerMetricsFromTracesProcessor(_tracer, _queryMeter, _dbMeter, _cacheMeter);

        _activitySource = new ActivitySource(DarkerSemanticConventions.SourceName);
        _activityListener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == DarkerSemanticConventions.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _ => { },
            ActivityStopped = _ => { }
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    [Fact]
    public void When_ending_internal_span_with_cache_outcome_should_dispatch_to_cache_meter()
    {
        //Arrange
        var activity = _activitySource.StartActivity("TestQuery query", ActivityKind.Internal);
        activity!.SetTag(DarkerSemanticConventions.QueryType, "TestQuery");
        activity.SetTag(DarkerSemanticConventions.Operation, "query");
        activity.SetTag(DarkerSemanticConventions.CacheOutcome, "miss");
        activity.Stop();

        //Act
        _darkerMetricsFromTracesProcessor.OnEnd(activity);
        _meterProvider.ForceFlush();

        //Assert - Internal span with cache outcome routes to cache meter and query meter
        _metrics.ShouldContain(m => m.Name == DarkerSemanticConventions.CacheRequestsMetricName);
        _metrics.ShouldContain(m => m.Name == DarkerSemanticConventions.QueryDurationMetricName);
    }

    [Fact]
    public void When_no_meter_enabled_should_short_circuit_and_not_throw()
    {
        // Dispose the subscribing provider first so no MeterProvider is listening to paramore.darker.
        // This ensures the meters created below report Enabled == false (NFR2 short-circuit guard).
        _meterProvider.Dispose();

        //Arrange - build a provider that does NOT subscribe to paramore.darker → Enabled == false
        using var disabledMeterFactory = new SimpleMeterFactory();
        using var disabledProvider = Sdk.CreateMeterProviderBuilder()
            .Build()!;

        var disabledQueryMeter = new QueryMeter(disabledMeterFactory, disabledProvider);
        var disabledDbMeter = new DbMeter(disabledMeterFactory, disabledProvider);
        var disabledCacheMeter = new CacheMeter(disabledMeterFactory, disabledProvider);

        disabledQueryMeter.Enabled.ShouldBeFalse();
        disabledDbMeter.Enabled.ShouldBeFalse();
        disabledCacheMeter.Enabled.ShouldBeFalse();

        using var disabledProcessor = new DarkerMetricsFromTracesProcessor(
            _tracer, disabledQueryMeter, disabledDbMeter, disabledCacheMeter);

        var activity = _activitySource.StartActivity("TestQuery query", ActivityKind.Internal);
        activity!.Stop();

        //Act & Assert - short-circuit must not throw
        Should.NotThrow(() => disabledProcessor.OnEnd(activity));
    }

    public void Dispose()
    {
        _darkerMetricsFromTracesProcessor.Dispose();
        _activityListener.Dispose();
        _activitySource.Dispose();
        _tracer.Dispose();
        _meterFactory.Dispose();
        _meterProvider.Dispose();
    }
}
