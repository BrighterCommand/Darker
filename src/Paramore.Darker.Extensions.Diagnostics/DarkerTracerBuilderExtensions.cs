using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Trace;
using Paramore.Darker.Extensions.Diagnostics.Observability;
using Paramore.Darker.Observability;

namespace Paramore.Darker.Extensions.Diagnostics;

/// <summary>
/// Extension methods for registering Darker instrumentation with the OpenTelemetry
/// <see cref="TracerProviderBuilder"/>.
/// </summary>
public static class DarkerTracerBuilderExtensions
{
    /// <summary>
    /// Adds Darker instrumentation to the <see cref="TracerProviderBuilder"/>.
    /// Constructs a <see cref="DarkerTracer"/>, registers it as a singleton
    /// <see cref="IAmADarkerTracer"/> in the DI container, and subscribes the
    /// <c>paramore.darker</c> <see cref="System.Diagnostics.ActivitySource"/> so that
    /// Darker query spans are collected by the OpenTelemetry SDK.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to configure.</param>
    /// <returns>The <paramref name="builder"/> for chaining.</returns>
    /// <remarks>
    /// Mirrors <c>BrighterTracerBuilderExtensions.AddBrighterInstrumentation</c>. Calling
    /// <c>AddDarkerInstrumentation()</c> on a <c>TracerProviderBuilder</c> is all that is needed
    /// to wire Darker query spans into an OpenTelemetry pipeline. The registered
    /// <see cref="IAmADarkerTracer"/> singleton can then be resolved from the DI container and
    /// passed to <see cref="QueryProcessor"/> so it creates spans for every query.
    /// </remarks>
    public static TracerProviderBuilder AddDarkerInstrumentation(this TracerProviderBuilder builder)
    {
        var tracer = new DarkerTracer();

        builder.ConfigureServices(services =>
            services.TryAddSingleton<IAmADarkerTracer>(tracer));

        builder.AddSource(tracer.ActivitySource.Name);

        builder.ConfigureServices(services =>
        {
            if (services.Any(sd => sd.ServiceType == typeof(IAmADarkerQueryMeter)) &&
                services.Any(sd => sd.ServiceType == typeof(IAmADarkerDbMeter)))
            {
                builder.AddProcessor(sp => new DarkerMetricsFromTracesProcessor(
                    sp.GetRequiredService<IAmADarkerTracer>(),
                    sp.GetRequiredService<IAmADarkerQueryMeter>(),
                    sp.GetRequiredService<IAmADarkerDbMeter>()));
            }
        });

        return builder;
    }
}
