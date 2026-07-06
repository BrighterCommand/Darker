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
using System.Linq;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Paramore.Darker.Extensions.Diagnostics.Observability;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Diagnostics.Tests;

[Collection("DarkerMeter")]
public class DbMeterRecordingTests : IDisposable
{
    private readonly List<Metric> _metrics;
    private readonly MeterProvider _meterProvider;
    private readonly DbMeter _dbMeter;
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

    public DbMeterRecordingTests()
    {
        _metrics = new List<Metric>();

        // Build the MeterProvider BEFORE creating the DbMeter so that the SDK's
        // MeterListener is registered and will pick up the histogram when it is published.
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(DarkerSemanticConventions.MeterName)
            .AddInMemoryExporter(_metrics)
            .Build()!;

        _meterFactory = new SimpleMeterFactory();
        _dbMeter = new DbMeter(_meterFactory, _meterProvider);

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
    public void When_recording_db_operation_should_record_duration_with_allowed_db_tags()
    {
        //Arrange
        var activity = _activitySource.StartActivity("orders select", ActivityKind.Client);
        activity!.SetTag(DarkerSemanticConventions.DbSystem, "postgresql");
        activity.SetTag(DarkerSemanticConventions.DbName, "orders");
        activity.SetTag(DarkerSemanticConventions.DbOperation, "select");
        activity.SetTag(DarkerSemanticConventions.DbSqlTable, "order");
        activity.SetTag(DarkerSemanticConventions.ServerAddress, "db-host");
        activity.SetTag(DarkerSemanticConventions.DbStatement, "SELECT * FROM order WHERE id = 1");
        activity.Stop();

        //Act
        _dbMeter.RecordClientOperation(activity);
        _meterProvider.ForceFlush();

        //Assert
        var durationMetric = _metrics.Single(m => m.Name == DarkerSemanticConventions.DbClientOperationDurationMetricName);

        var points = new List<MetricPoint>();
        foreach (var point in durationMetric.GetMetricPoints())
            points.Add(point);
        points.Count.ShouldBe(1);

        var tagKeys = new List<string>();
        foreach (var tag in points[0].Tags)
            tagKeys.Add(tag.Key);

        tagKeys.ShouldContain(DarkerSemanticConventions.DbSystem);
        tagKeys.ShouldContain(DarkerSemanticConventions.DbName);
        tagKeys.ShouldContain(DarkerSemanticConventions.DbOperation);
        tagKeys.ShouldContain(DarkerSemanticConventions.DbSqlTable);
        tagKeys.ShouldContain(DarkerSemanticConventions.ServerAddress);
        tagKeys.ShouldNotContain(DarkerSemanticConventions.DbStatement);
    }

    [Fact]
    public void When_error_type_tag_present_should_surface_as_metric_dimension()
    {
        //Arrange
        var activity = _activitySource.StartActivity("orders select", ActivityKind.Client);
        activity!.SetTag(DarkerSemanticConventions.DbSystem, "postgresql");
        activity.SetTag(DarkerSemanticConventions.DbOperation, "select");
        activity.SetTag(DarkerSemanticConventions.ErrorType, "System.TimeoutException");
        activity.Stop();

        //Act
        _dbMeter.RecordClientOperation(activity);
        _meterProvider.ForceFlush();

        //Assert
        var durationMetric = _metrics.Single(m => m.Name == DarkerSemanticConventions.DbClientOperationDurationMetricName);

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
        _dbMeter.Enabled.ShouldBeTrue();
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _activitySource.Dispose();
        _meterFactory.Dispose();
        _meterProvider.Dispose();
    }
}
