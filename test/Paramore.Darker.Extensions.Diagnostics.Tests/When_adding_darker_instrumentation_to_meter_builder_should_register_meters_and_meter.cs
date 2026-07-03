using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Metrics;
using Paramore.Darker.Extensions.Diagnostics.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Diagnostics.Tests;

[Collection("DarkerMeter")]
public class AddDarkerInstrumentationToMeterBuilderTests
{
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
    public void When_adding_darker_instrumentation_to_meter_builder_should_register_meters_and_meter()
    {
        //Arrange
        var metrics = new List<Metric>();
        var services = new ServiceCollection();

        // In production (ASP.NET Core / Generic Host) IMeterFactory is registered by the host.
        // Register a test stub so QueryMeter and DbMeter can be activated from DI.
        services.TryAddSingleton<IMeterFactory, TestMeterFactory>();

        services.AddOpenTelemetry()
            .WithMetrics(b => b
                .AddDarkerInstrumentation()
                .AddInMemoryExporter(metrics));

        using var sp = services.BuildServiceProvider();

        //Act
        // Resolve MeterProvider to activate collection before resolving meters
        _ = sp.GetRequiredService<MeterProvider>();

        var queryMeter = sp.GetRequiredService<IAmADarkerQueryMeter>();
        var dbMeter = sp.GetRequiredService<IAmADarkerDbMeter>();

        //Assert
        queryMeter.ShouldNotBeNull();
        dbMeter.ShouldNotBeNull();
        queryMeter.Enabled.ShouldBeTrue();
        dbMeter.Enabled.ShouldBeTrue();

        // Resolving IAmADarkerQueryMeter twice returns the same singleton instance
        var queryMeter2 = sp.GetRequiredService<IAmADarkerQueryMeter>();
        queryMeter2.ShouldBeSameAs(queryMeter);
    }
}
