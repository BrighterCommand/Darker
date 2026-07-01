using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using System;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_creating_tracer_without_listener_should_expose_source_and_return_null_span
    {
        [Fact]
        public void Should_expose_activity_source_named_after_semantic_conventions()
        {
            // Arrange
            using var tracer = new DarkerTracer();

            // Act
            var name = tracer.ActivitySource.Name;

            // Assert
            name.ShouldBe(DarkerSemanticConventions.SourceName);
        }

        [Fact]
        public void Should_return_null_span_when_no_listener_is_registered()
        {
            // Arrange — no ActivityListener registered; zero-overhead path
            using var tracer = new DarkerTracer();
            var query = new SomeQuery();

            // Act
            var span = tracer.CreateQuerySpan(query);

            // Assert
            span.ShouldBeNull();
        }

        [Fact]
        public void Should_implement_IAmADarkerTracer_which_is_IDisposable()
        {
            // Arrange
            var tracer = new DarkerTracer();

            // Act — dispose must not throw; cast to interface to confirm contract
            IAmADarkerTracer asInterface = tracer;
            Should.NotThrow(() => asInterface.Dispose());

            // Assert
            tracer.ShouldBeAssignableTo<IAmADarkerTracer>();
        }
    }
}
