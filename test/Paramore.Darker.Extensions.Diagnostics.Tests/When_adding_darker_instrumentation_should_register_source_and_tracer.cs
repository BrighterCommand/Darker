using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Paramore.Darker;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Diagnostics.Tests;

public class AddDarkerInstrumentationTests
{
    private sealed record SampleQuery : IQuery<string>;

    [Fact]
    public void When_adding_darker_instrumentation_should_register_source_and_tracer()
    {
        //Arrange
        var exportedItems = new List<Activity>();
        var services = new ServiceCollection();
        services.AddOpenTelemetry()
            .WithTracing(b => b
                .AddDarkerInstrumentation()
                .AddInMemoryExporter(exportedItems));

        //Act
        using var sp = services.BuildServiceProvider();

        // Resolve TracerProvider to activate the ActivityListener before creating spans
        var tracerProvider = sp.GetRequiredService<TracerProvider>();

        // Resolve IAmADarkerTracer from the DI container
        var tracer = sp.GetRequiredService<IAmADarkerTracer>();

        // Create and end a span on the tracer so the source is exercised
        var span = tracer.CreateQuerySpan(new SampleQuery());
        tracer.EndSpan(span);

        // Flush so the in-memory exporter captures the span
        tracerProvider.ForceFlush();

        //Assert
        // Source is subscribed: the span was exported
        exportedItems.Count.ShouldBe(1);

        // IAmADarkerTracer is registered as a singleton — resolving twice returns the same instance
        var tracer2 = sp.GetRequiredService<IAmADarkerTracer>();
        tracer2.ShouldBeSameAs(tracer);
    }
}
