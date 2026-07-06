using System.Diagnostics;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    [Collection("DarkerActivitySource")]
    public class When_creating_query_span_with_query_context_should_copy_spancontext_bag_entries
    {
        [Fact]
        public void Should_copy_spancontext_prefixed_bag_entries_onto_span_when_QueryContext_option_is_set()
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

            var context = new QueryContext();
            context.Bag["spancontext.tenant"] = "acme";
            context.Bag["other"] = "x";

            // Act
            var span = tracer.CreateQuerySpan(query, null, context, InstrumentationOptions.QueryContext);

            // Assert
            try
            {
                span.ShouldNotBeNull();

                var tenantTag = span.GetTagItem("spancontext.tenant");
                tenantTag.ShouldBe("acme");

                var otherTag = span.GetTagItem("other");
                otherTag.ShouldBeNull();
            }
            finally
            {
                span?.Stop();
            }
        }

        [Fact]
        public void Should_not_copy_spancontext_bag_entries_when_QueryContext_option_is_absent()
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

            var context = new QueryContext();
            context.Bag["spancontext.tenant"] = "acme";

            // Act
            var span = tracer.CreateQuerySpan(query, null, context, InstrumentationOptions.QueryInformation);

            // Assert
            try
            {
                span.ShouldNotBeNull();

                var tenantTag = span.GetTagItem("spancontext.tenant");
                tenantTag.ShouldBeNull();
            }
            finally
            {
                span?.Stop();
            }
        }
    }
}
