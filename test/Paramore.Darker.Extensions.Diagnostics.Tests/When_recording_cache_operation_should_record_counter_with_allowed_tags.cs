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
public class CacheMeterRecordingTests : IDisposable
{
    private readonly List<Metric> _metrics;
    private readonly MeterProvider _meterProvider;
    private readonly CacheMeter _cacheMeter;
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;
    private readonly SimpleMeterFactory _meterFactory;

    /// <summary>
    /// Minimal <see cref="IMeterFactory"/> that creates bare <see cref="Meter"/> instances.
    /// The OTel SDK's <see cref="MeterProvider"/> picks them up via its MeterListener once
    /// the counter instrument is published.
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

    public CacheMeterRecordingTests()
    {
        _metrics = new List<Metric>();

        // Build the MeterProvider BEFORE creating the CacheMeter so that the SDK's
        // MeterListener is registered and will pick up the counter when it is published.
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(DarkerSemanticConventions.MeterName)
            .AddInMemoryExporter(_metrics)
            .Build()!;

        _meterFactory = new SimpleMeterFactory();
        _cacheMeter = new CacheMeter(_meterFactory, _meterProvider);

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
    public void When_recording_cache_operation_should_record_counter_with_allowed_tags()
    {
        //Arrange - activity with CacheOutcome "hit" and querytype; QueryId is high-cardinality and must be excluded
        var activity = _activitySource.StartActivity("TestQuery caching", ActivityKind.Internal);
        activity!.SetTag(DarkerSemanticConventions.QueryType, "TestQuery");
        activity.SetTag(DarkerSemanticConventions.CacheOutcome, "hit");
        activity.SetTag(DarkerSemanticConventions.QueryId, "some-high-cardinality-id");
        activity.Stop();

        //Act
        _cacheMeter.RecordCacheOperation(activity);
        _meterProvider.ForceFlush();

        //Assert - one measurement on the cache.requests counter with only the allowed tags
        var counterMetric = _metrics.Single(m => m.Name == DarkerSemanticConventions.CacheRequestsMetricName);

        var points = new List<MetricPoint>();
        foreach (var point in counterMetric.GetMetricPoints())
            points.Add(point);
        points.Count.ShouldBe(1);

        var tagKeys = new List<string>();
        foreach (var tag in points[0].Tags)
            tagKeys.Add(tag.Key);

        // cache.outcome and querytype are allowed and must be present
        tagKeys.ShouldContain(DarkerSemanticConventions.CacheOutcome);
        tagKeys.ShouldContain(DarkerSemanticConventions.QueryType);
        // high-cardinality queryid must be filtered out
        tagKeys.ShouldNotContain(DarkerSemanticConventions.QueryId);
    }

    [Fact]
    public void When_activity_has_no_cache_outcome_tag_should_record_nothing()
    {
        //Arrange - activity WITHOUT the CacheOutcome tag → RecordCacheOperation is a no-op
        var activity = _activitySource.StartActivity("TestQuery caching", ActivityKind.Internal);
        activity!.SetTag(DarkerSemanticConventions.QueryType, "TestQuery");
        // Note: CacheOutcome tag intentionally absent
        activity.Stop();

        //Act
        _cacheMeter.RecordCacheOperation(activity);
        _meterProvider.ForceFlush();

        //Assert - no measurement recorded; metric must not be present
        _metrics.ShouldNotContain(m => m.Name == DarkerSemanticConventions.CacheRequestsMetricName);
    }

    [Fact]
    public void When_no_meter_provider_subscribes_to_paramore_darker_enabled_should_be_false()
    {
        // Dispose the subscribing provider first so no MeterProvider is listening to paramore.darker.
        _meterProvider.Dispose();

        //Arrange - build a provider that does NOT subscribe to paramore.darker → Enabled == false
        using var disabledMeterFactory = new SimpleMeterFactory();
        using var disabledProvider = Sdk.CreateMeterProviderBuilder().Build()!;

        //Act
        var cacheMeter = new CacheMeter(disabledMeterFactory, disabledProvider);

        //Assert
        cacheMeter.Enabled.ShouldBeFalse();
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _activitySource.Dispose();
        _meterFactory.Dispose();
        _meterProvider.Dispose();
    }
}
