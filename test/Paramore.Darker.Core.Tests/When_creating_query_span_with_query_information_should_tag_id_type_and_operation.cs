using System.Diagnostics;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_creating_query_span_with_query_information_should_tag_id_type_and_operation
    {
        [Fact]
        public void Should_tag_queryid_querytype_and_operation_for_query_deriving_from_Query_base()
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
            var query = new QueryWithDefaultId();

            // Act
            var span = tracer.CreateQuerySpan(query, options: InstrumentationOptions.QueryInformation);

            // Assert
            try
            {
                span.ShouldNotBeNull();
                span.IsAllDataRequested.ShouldBeTrue();

                var queryId = span.GetTagItem(DarkerSemanticConventions.QueryId);
                queryId.ShouldNotBeNull();
                queryId.ShouldBe(query.Id);

                var queryType = span.GetTagItem(DarkerSemanticConventions.QueryType);
                queryType.ShouldBe(query.GetType().FullName);

                var operation = span.GetTagItem(DarkerSemanticConventions.Operation);
                operation.ShouldBe("query");
            }
            finally
            {
                span?.Stop();
            }
        }

        [Fact]
        public void Should_tag_querytype_and_operation_but_not_queryid_for_query_implementing_IQuery_directly()
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
            var span = tracer.CreateQuerySpan(query, options: InstrumentationOptions.QueryInformation);

            // Assert
            try
            {
                span.ShouldNotBeNull();
                span.IsAllDataRequested.ShouldBeTrue();

                var queryId = span.GetTagItem(DarkerSemanticConventions.QueryId);
                queryId.ShouldBeNull();

                var queryType = span.GetTagItem(DarkerSemanticConventions.QueryType);
                queryType.ShouldBe(query.GetType().FullName);

                var operation = span.GetTagItem(DarkerSemanticConventions.Operation);
                operation.ShouldBe("query");
            }
            finally
            {
                span?.Stop();
            }
        }
    }
}
