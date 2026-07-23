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
    /// Registers <see cref="IAmADarkerQueryMeter"/>, <see cref="IAmADarkerDbMeter"/>, and
    /// (when <paramref name="emitCacheMetrics"/> is <c>true</c>) <see cref="IAmADarkerCacheMeter"/>
    /// as singletons in the DI container and subscribes the <c>paramore.darker</c>
    /// <see cref="System.Diagnostics.Metrics.Meter"/> so that Darker metric instruments
    /// are collected by the OpenTelemetry SDK.
    /// </summary>
    /// <param name="builder">The <see cref="MeterProviderBuilder"/> to configure.</param>
    /// <param name="emitCacheMetrics">
    /// When <c>true</c> (the default), registers a <see cref="CacheMeter"/> that emits
    /// <c>paramore.darker.cache.requests</c> counter measurements. Set to <c>false</c> to
    /// register a no-op <see cref="IAmADarkerCacheMeter"/> (<c>Enabled == false</c>) and
    /// suppress cache counter emission — useful when the underlying cache (HybridCache /
    /// FusionCache) already emits equivalent metrics and double-reporting should be avoided
    /// (FR10, Resolved Decision 4). This toggle is independent of
    /// <see cref="Paramore.Darker.Observability.InstrumentationOptions"/>, which gates
    /// span-attribute groups rather than metric emission.
    /// </param>
    /// <returns>The <paramref name="builder"/> for chaining.</returns>
    /// <remarks>
    /// Mirrors <c>BrighterMetricsBuilderExtensions.AddBrighterInstrumentation</c>. Calling
    /// <c>AddDarkerInstrumentation()</c> on a <c>MeterProviderBuilder</c> is all that is needed
    /// to wire Darker query, DB, and cache metrics into an OpenTelemetry metrics pipeline.
    /// The registered meters are singletons and can be resolved from the DI container to be
    /// passed into the metrics-from-traces processor (ADR 0021).
    /// </remarks>
    public static MeterProviderBuilder AddDarkerInstrumentation(
        this MeterProviderBuilder builder,
        bool emitCacheMetrics = true)
    {
        builder.ConfigureServices(services =>
        {
            services.TryAddSingleton<IAmADarkerQueryMeter, QueryMeter>();
            services.TryAddSingleton<IAmADarkerDbMeter, DbMeter>();

            if (emitCacheMetrics)
                services.TryAddSingleton<IAmADarkerCacheMeter, CacheMeter>();
            else
                services.TryAddSingleton<IAmADarkerCacheMeter>(new NoOpCacheMeter());
        });

        builder.AddMeter(DarkerSemanticConventions.MeterName);

        return builder;
    }
}
