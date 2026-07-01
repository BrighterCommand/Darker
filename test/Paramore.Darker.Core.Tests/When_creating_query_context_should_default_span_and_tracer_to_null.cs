using System.Diagnostics;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_creating_query_context_should_default_span_and_tracer_to_null
    {
        [Fact]
        public void Should_default_span_and_tracer_to_null()
        {
            // Arrange
            var context = new QueryContext();

            // Assert
            context.Span.ShouldBeNull();
            context.Tracer.ShouldBeNull();
        }

        [Fact]
        public void Should_round_trip_span_through_interface()
        {
            // Arrange
            var activity = new Activity("test.span");
            IQueryContext context = new QueryContext();

            // Act
            context.Span = activity;

            // Assert
            context.Span.ShouldBeSameAs(activity);
        }

        [Fact]
        public void Should_round_trip_tracer_through_interface()
        {
            // Arrange
            IQueryContext context = new QueryContext();
            var tracer = new FakeTracer();

            // Act
            context.Tracer = tracer;

            // Assert
            context.Tracer.ShouldBeSameAs(tracer);
        }

        private sealed class FakeTracer : IAmADarkerTracer
        {
            public void Dispose() { }
        }
    }
}
