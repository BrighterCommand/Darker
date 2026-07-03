using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Paramore.Darker.Extensions.Diagnostics.Observability;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Diagnostics.Tests;

[Collection("DarkerMeter")]
public class QueryMeterRecordingTests : IDisposable
{
    private readonly List<Metric> _metrics;
    private readonly MeterProvider _meterProvider;
    private readonly QueryMeter _queryMeter;
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;
    private readonly SimpleMeterFactory _meterFactory;

    /// <summary>
    /// Minimal <see cref="IMeterFactory"/> that creates bare <see cref="Meter"/> instances.
    /// The OTel SDK's <see cref="MeterProvider"/> picks them up via its MeterListener once
    /// the histogram instrument is published.
    /// </summary>
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

    public QueryMeterRecordingTests()
    {
        _metrics = new List<Metric>();

        // Build the MeterProvider BEFORE creating the QueryMeter so that the SDK's
        // MeterListener is registered and will pick up the histogram when it is published.
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(DarkerSemanticConventions.MeterName)
            .AddInMemoryExporter(_metrics)
            .Build()!;

        _meterFactory = new SimpleMeterFactory();
        _queryMeter = new QueryMeter(_meterFactory, _meterProvider);

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
    public void When_recording_query_operation_should_record_duration_with_allowed_query_tags()
    {
        //Arrange
        var activity = _activitySource.StartActivity("TestQuery query", ActivityKind.Internal);
        activity!.SetTag(DarkerSemanticConventions.QueryType, "TestQuery");
        activity.SetTag(DarkerSemanticConventions.Operation, "query");
        activity.SetTag(DarkerSemanticConventions.QueryBody, "{\"id\":1}");
        activity.Stop();

        //Act
        _queryMeter.RecordQueryOperation(activity);
        _meterProvider.ForceFlush();

        //Assert
        var durationMetric = _metrics.Single(m => m.Name == DarkerSemanticConventions.QueryDurationMetricName);

        var points = new List<MetricPoint>();
        foreach (var point in durationMetric.GetMetricPoints())
            points.Add(point);
        points.Count.ShouldBe(1);

        var tagKeys = new List<string>();
        foreach (var tag in points[0].Tags)
            tagKeys.Add(tag.Key);

        tagKeys.ShouldContain(DarkerSemanticConventions.QueryType);
        tagKeys.ShouldContain(DarkerSemanticConventions.Operation);
        tagKeys.ShouldNotContain(DarkerSemanticConventions.QueryBody);
    }

    [Fact]
    public void When_error_type_tag_present_should_surface_as_metric_dimension()
    {
        //Arrange
        var activity = _activitySource.StartActivity("FailingQuery query", ActivityKind.Internal);
        activity!.SetTag(DarkerSemanticConventions.QueryType, "FailingQuery");
        activity.SetTag(DarkerSemanticConventions.Operation, "query");
        activity.SetTag(DarkerSemanticConventions.ErrorType, "System.InvalidOperationException");
        activity.Stop();

        //Act
        _queryMeter.RecordQueryOperation(activity);
        _meterProvider.ForceFlush();

        //Assert
        var durationMetric = _metrics.Single(m => m.Name == DarkerSemanticConventions.QueryDurationMetricName);

        var points = new List<MetricPoint>();
        foreach (var point in durationMetric.GetMetricPoints())
            points.Add(point);
        points.Count.ShouldBe(1);

        var tagKeys = new List<string>();
        foreach (var tag in points[0].Tags)
            tagKeys.Add(tag.Key);

        tagKeys.ShouldContain(DarkerSemanticConventions.ErrorType);
    }

    [Fact]
    public void When_meter_subscribed_should_report_enabled()
    {
        //Assert
        _queryMeter.Enabled.ShouldBeTrue();
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _activitySource.Dispose();
        _meterFactory.Dispose();
        _meterProvider.Dispose();
    }
}
