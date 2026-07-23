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
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Metrics;
using Paramore.Darker.Extensions.Diagnostics.Observability;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Diagnostics.Tests;

[Collection("DarkerMeter")]
public class AddDarkerInstrumentationCacheMeterToggleTests
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
    public void When_adding_darker_instrumentation_with_default_should_resolve_cache_meter()
    {
        //Arrange
        var metrics = new List<Metric>();
        var services = new ServiceCollection();

        // In production (ASP.NET Core / Generic Host) IMeterFactory is registered by the host.
        // Register a test stub so CacheMeter can be activated from DI.
        services.TryAddSingleton<IMeterFactory, TestMeterFactory>();

        services.AddOpenTelemetry()
            .WithMetrics(b => b
                .AddDarkerInstrumentation()
                .AddInMemoryExporter(metrics));

        using var sp = services.BuildServiceProvider();

        //Act
        // Resolve MeterProvider to activate collection before resolving meters
        _ = sp.GetRequiredService<MeterProvider>();
        var cacheMeter = sp.GetRequiredService<IAmADarkerCacheMeter>();

        //Assert — the real CacheMeter is registered and is enabled
        cacheMeter.ShouldBeOfType<CacheMeter>();
        cacheMeter.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void When_adding_darker_instrumentation_with_cache_metrics_disabled_should_register_noop_cache_meter()
    {
        //Arrange
        var metrics = new List<Metric>();
        var services = new ServiceCollection();

        services.TryAddSingleton<IMeterFactory, TestMeterFactory>();

        services.AddOpenTelemetry()
            .WithMetrics(b => b
                .AddDarkerInstrumentation(emitCacheMetrics: false)
                .AddInMemoryExporter(metrics));

        using var sp = services.BuildServiceProvider();
        var meterProvider = sp.GetRequiredService<MeterProvider>();

        //Act
        var cacheMeter = sp.GetRequiredService<IAmADarkerCacheMeter>();

        //Assert — a no-op meter is registered with Enabled == false
        cacheMeter.Enabled.ShouldBeFalse();

        // Routing a cache-outcome span through the meter records nothing on paramore.darker.cache.requests
        using var activity = new Activity("TestQuery caching");
        activity.Start();
        activity.SetTag(DarkerSemanticConventions.CacheOutcome, "hit");
        activity.Stop();

        cacheMeter.RecordCacheOperation(activity);
        meterProvider.ForceFlush();

        metrics.ShouldNotContain(m => m.Name == DarkerSemanticConventions.CacheRequestsMetricName);
    }
}
