using System.Diagnostics;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_creating_query_span_with_query_body_should_serialise_runtime_type
    {
        [Fact]
        public void Should_emit_query_body_tag_containing_concrete_property_values_when_query_body_flag_is_set()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "paramore.darker",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(listener);
            using var tracer = new DarkerTracer();
            var query = new QueryWithNameProperty("Alice");

            // Act
            var span = tracer.CreateQuerySpan(query, options: InstrumentationOptions.QueryBody);

            // Assert
            try
            {
                span.ShouldNotBeNull();
                span.IsAllDataRequested.ShouldBeTrue();

                var bodyTag = span.GetTagItem(DarkerSemanticConventions.QueryBody) as string;
                bodyTag.ShouldNotBeNull();
                bodyTag.ShouldContain("Alice");
            }
            finally
            {
                span?.Stop();
            }
        }

        [Fact]
        public void Should_not_emit_query_body_tag_when_query_body_flag_is_absent()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "paramore.darker",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(listener);
            using var tracer = new DarkerTracer();
            var query = new QueryWithNameProperty("Bob");

            // Act
            var span = tracer.CreateQuerySpan(query, options: InstrumentationOptions.QueryInformation);

            // Assert
            try
            {
                span.ShouldNotBeNull();

                var bodyTag = span.GetTagItem(DarkerSemanticConventions.QueryBody);
                bodyTag.ShouldBeNull();
            }
            finally
            {
                span?.Stop();
            }
        }
    }
}
