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

using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Paramore.Darker;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Diagnostics.Tests;

/// <summary>
/// Verifies that <see cref="DarkerTracerBuilderExtensions.AddDarkerInstrumentation"/> adds a
/// <c>DarkerMetricsFromTracesProcessor</c> to the tracer pipeline only when both
/// <c>IAmADarkerQueryMeter</c> and <c>IAmADarkerDbMeter</c> are registered in DI (ADR 0018 §6,
/// NFR2/AC8).
/// </summary>
[Collection("DarkerMeter")]
public class TracerInstrumentationWithMetersTests
{
    private sealed record SampleQuery : IQuery<string>;

    /// <summary>
    /// Minimal <see cref="IMeterFactory"/> that creates bare <see cref="Meter"/> instances.
    /// In production, <see cref="IMeterFactory"/> is registered by the ASP.NET Core / Generic Host
    /// infrastructure. This stub fulfils that role in a bare <see cref="ServiceCollection"/> test.
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

    [Fact]
    public void When_adding_tracer_instrumentation_should_add_metrics_processor_only_when_meters_registered()
    {
        //Arrange — both tracer and meter builders wired
        var metrics = new List<Metric>();
        var services = new ServiceCollection();

        // IMeterFactory is provided by the Generic Host in production; supply a stub here
        services.TryAddSingleton<IMeterFactory, TestMeterFactory>();

        // WithMetrics must be called BEFORE WithTracing so that meter registrations are visible
        // when AddDarkerInstrumentation's ConfigureServices callback checks for them (the callback
        // runs immediately at call time, not lazily at provider-build time).
        services.AddOpenTelemetry()
            .WithMetrics(b => b.AddDarkerInstrumentation().AddInMemoryExporter(metrics))
            .WithTracing(b => b.AddDarkerInstrumentation());

        using var sp = services.BuildServiceProvider();

        // Resolve both providers to activate their listeners before creating spans
        var tracerProvider = sp.GetRequiredService<TracerProvider>();
        var meterProvider = sp.GetRequiredService<MeterProvider>();
        var tracer = sp.GetRequiredService<IAmADarkerTracer>();

        //Act
        var span = tracer.CreateQuerySpan(new SampleQuery(), options: InstrumentationOptions.QueryInformation);
        tracer.EndSpan(span);

        // Flush tracer pipeline (causes the processor to complete) then flush meter exporter
        tracerProvider.ForceFlush();
        meterProvider.ForceFlush();

        //Assert — DarkerMetricsFromTracesProcessor was added; the query duration metric was recorded
        metrics.ShouldContain(m => m.Name == DarkerSemanticConventions.QueryDurationMetricName);
    }

    [Fact]
    public void When_adding_tracer_instrumentation_only_should_not_add_processor_or_record_metrics()
    {
        //Arrange — tracer only wired, no meter builder (NFR2/AC8)
        var services = new ServiceCollection();
        services.AddOpenTelemetry()
            .WithTracing(b => b.AddDarkerInstrumentation());

        using var sp = services.BuildServiceProvider();

        // Resolve TracerProvider to activate the ActivityListener before creating spans
        var tracerProvider = sp.GetRequiredService<TracerProvider>();
        var tracer = sp.GetRequiredService<IAmADarkerTracer>();

        // Create a standalone MeterProvider to observe any metrics that might be emitted
        var noMeterMetrics = new List<Metric>();
        using var standaloneMeterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(DarkerSemanticConventions.MeterName)
            .AddInMemoryExporter(noMeterMetrics)
            .Build()!;

        //Act — must not throw
        var span = tracer.CreateQuerySpan(new SampleQuery(), options: InstrumentationOptions.QueryInformation);
        tracer.EndSpan(span);

        tracerProvider.ForceFlush();
        standaloneMeterProvider.ForceFlush();

        //Assert — no processor was added; no metrics were recorded
        noMeterMetrics.ShouldBeEmpty();
    }
}
