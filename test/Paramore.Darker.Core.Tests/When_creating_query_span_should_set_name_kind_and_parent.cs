using System.Diagnostics;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    [Collection("DarkerActivitySource")]
    public class When_creating_query_span_should_set_name_kind_and_parent
    {
        [Fact]
        public void Should_return_activity_with_query_type_display_name_and_internal_kind()
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
            var query = new SomeQuery();

            // Act
            // Body-free options: this test asserts name/kind/parent only and must not serialise the
            // query body (which would lock the process-global QueryLoggingJsonOptions.Options static).
            var span = tracer.CreateQuerySpan(query, options: InstrumentationOptions.QueryInformation);

            // Assert
            try
            {
                span.ShouldNotBeNull();
                span.DisplayName.ShouldBe("SomeQuery query");
                span.Kind.ShouldBe(ActivityKind.Internal);
            }
            finally
            {
                span?.Stop();
            }
        }

        [Fact]
        public void Should_nest_span_under_explicit_parent_when_parent_activity_is_provided()
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
            var query = new SomeQuery();
            using var parentActivity = new Activity("parent").Start();

            // Act
            // Body-free options (see note above): assert parenting without locking the JSON static.
            var span = tracer.CreateQuerySpan(query, parentActivity, options: InstrumentationOptions.QueryInformation);

            // Assert
            try
            {
                span.ShouldNotBeNull();
                span.ParentId.ShouldBe(parentActivity.Id);
            }
            finally
            {
                span?.Stop();
                parentActivity.Stop();
            }
        }

        [Fact]
        public void Should_set_Activity_Current_to_returned_span_after_creation()
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
            var query = new SomeQuery();

            // Act
            // Body-free options: this test asserts name/kind/parent only and must not serialise the
            // query body (which would lock the process-global QueryLoggingJsonOptions.Options static).
            var span = tracer.CreateQuerySpan(query, options: InstrumentationOptions.QueryInformation);

            // Assert
            try
            {
                span.ShouldNotBeNull();
                Activity.Current.ShouldBe(span);
            }
            finally
            {
                span?.Stop();
            }
        }
    }
}
