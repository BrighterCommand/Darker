using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Metrics;
using Paramore.Darker.Extensions.Diagnostics.Observability;
using Paramore.Darker.Observability;

namespace Paramore.Darker.Extensions.Diagnostics;

/// <summary>
/// Extension methods for registering Darker metric instrumentation with the OpenTelemetry
/// <see cref="MeterProviderBuilder"/>.
/// </summary>
public static class DarkerMetricsBuilderExtensions
{
    /// <summary>
    /// Adds Darker instrumentation to the <see cref="MeterProviderBuilder"/>.
    /// Registers <see cref="IAmADarkerQueryMeter"/> and <see cref="IAmADarkerDbMeter"/> as
    /// singletons in the DI container and subscribes the <c>paramore.darker</c>
    /// <see cref="System.Diagnostics.Metrics.Meter"/> so that Darker metric instruments
    /// are collected by the OpenTelemetry SDK.
    /// </summary>
    /// <param name="builder">The <see cref="MeterProviderBuilder"/> to configure.</param>
    /// <returns>The <paramref name="builder"/> for chaining.</returns>
    /// <remarks>
    /// Mirrors <c>BrighterMetricsBuilderExtensions.AddBrighterInstrumentation</c>. Calling
    /// <c>AddDarkerInstrumentation()</c> on a <c>MeterProviderBuilder</c> is all that is needed
    /// to wire Darker query and DB duration histograms into an OpenTelemetry metrics pipeline.
    /// The registered meters are singletons and can be resolved from the DI container to be
    /// passed into the metrics-from-traces processor (ADR 0018).
    /// </remarks>
    public static MeterProviderBuilder AddDarkerInstrumentation(this MeterProviderBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.TryAddSingleton<IAmADarkerQueryMeter, QueryMeter>();
            services.TryAddSingleton<IAmADarkerDbMeter, DbMeter>();
        });

        builder.AddMeter(DarkerSemanticConventions.MeterName);

        return builder;
    }
}
