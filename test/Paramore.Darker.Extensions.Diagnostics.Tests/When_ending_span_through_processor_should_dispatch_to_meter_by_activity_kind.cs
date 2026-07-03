#region Licence

/* The MIT License (MIT)
Copyright © 2025 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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
public class MetricsFromTracesProcessorTests : IDisposable
{
    private readonly List<Metric> _metrics;
    private readonly MeterProvider _meterProvider;
    private readonly QueryMeter _queryMeter;
    private readonly DbMeter _dbMeter;
    private readonly IAmADarkerTracer _tracer;
    private readonly DarkerMetricsFromTracesProcessor _processor;
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

    public MetricsFromTracesProcessorTests()
    {
        _metrics = new List<Metric>();

        // Build the MeterProvider BEFORE creating the meters so that the SDK's
        // MeterListener is registered and will pick up the histograms when they are published.
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(DarkerSemanticConventions.MeterName)
            .AddInMemoryExporter(_metrics)
            .Build()!;

        _meterFactory = new SimpleMeterFactory();
        _queryMeter = new QueryMeter(_meterFactory, _meterProvider);
        _dbMeter = new DbMeter(_meterFactory, _meterProvider);

        _tracer = new DarkerTracer();

        _processor = new DarkerMetricsFromTracesProcessor(_tracer, _queryMeter, _dbMeter);

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
    public void When_ending_span_through_processor_should_dispatch_to_meter_by_activity_kind()
    {
        //Arrange
        var activity = _activitySource.StartActivity("TestQuery query", ActivityKind.Internal);
        activity!.SetTag(DarkerSemanticConventions.QueryType, "TestQuery");
        activity.SetTag(DarkerSemanticConventions.Operation, "query");
        activity.Stop();

        //Act
        _processor.OnEnd(activity);
        _meterProvider.ForceFlush();

        //Assert - Internal span routes to query meter only
        _metrics.ShouldContain(m => m.Name == DarkerSemanticConventions.QueryDurationMetricName);
        _metrics.ShouldNotContain(m => m.Name == DarkerSemanticConventions.DbClientOperationDurationMetricName);
    }

    [Fact]
    public void When_ending_client_span_through_processor_should_record_db_metric_only()
    {
        //Arrange
        var activity = _activitySource.StartActivity("orders select", ActivityKind.Client);
        activity!.SetTag(DarkerSemanticConventions.DbSystem, "postgresql");
        activity.SetTag(DarkerSemanticConventions.DbOperation, "select");
        activity.Stop();

        //Act
        _processor.OnEnd(activity);
        _meterProvider.ForceFlush();

        //Assert - Client span routes to db meter only
        _metrics.ShouldContain(m => m.Name == DarkerSemanticConventions.DbClientOperationDurationMetricName);
        _metrics.ShouldNotContain(m => m.Name == DarkerSemanticConventions.QueryDurationMetricName);
    }

    [Fact]
    public void When_span_from_different_source_should_not_record_any_metrics()
    {
        //Arrange
        using var otherSource = new ActivitySource("other.source");
        using var otherListener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "other.source",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _ => { },
            ActivityStopped = _ => { }
        };
        ActivitySource.AddActivityListener(otherListener);

        var activity = otherSource.StartActivity("something", ActivityKind.Internal);
        activity!.Stop();

        //Act
        _processor.OnEnd(activity);
        _meterProvider.ForceFlush();

        //Assert - foreign source spans are ignored
        _metrics.ShouldNotContain(m => m.Name == DarkerSemanticConventions.QueryDurationMetricName);
        _metrics.ShouldNotContain(m => m.Name == DarkerSemanticConventions.DbClientOperationDurationMetricName);
    }

    [Fact]
    public void When_neither_meter_enabled_should_short_circuit_and_not_throw()
    {
        // Dispose the subscribing provider first so no MeterProvider is listening to paramore.darker.
        // This ensures the histograms created below report Enabled == false (NFR2 short-circuit guard).
        _meterProvider.Dispose();

        //Arrange - build a provider that does NOT subscribe to paramore.darker → Enabled == false
        using var disabledMeterFactory = new SimpleMeterFactory();
        using var disabledProvider = Sdk.CreateMeterProviderBuilder()
            .Build()!;

        var disabledQueryMeter = new QueryMeter(disabledMeterFactory, disabledProvider);
        var disabledDbMeter = new DbMeter(disabledMeterFactory, disabledProvider);

        disabledQueryMeter.Enabled.ShouldBeFalse();
        disabledDbMeter.Enabled.ShouldBeFalse();

        using var disabledProcessor = new DarkerMetricsFromTracesProcessor(_tracer, disabledQueryMeter, disabledDbMeter);

        var activity = _activitySource.StartActivity("TestQuery query", ActivityKind.Internal);
        activity!.Stop();

        //Act & Assert - short-circuit must not throw
        Should.NotThrow(() => disabledProcessor.OnEnd(activity));
    }

    public void Dispose()
    {
        _processor.Dispose();
        _activityListener.Dispose();
        _activitySource.Dispose();
        _tracer.Dispose();
        _meterFactory.Dispose();
        _meterProvider.Dispose();
    }
}
