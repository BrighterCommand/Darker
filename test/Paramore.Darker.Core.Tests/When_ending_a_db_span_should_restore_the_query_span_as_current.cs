using System.Diagnostics;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    [Collection("DarkerActivitySource")]
    public class DbSpanEndingTests
    {
        [Fact]
        public void When_ending_a_db_span_should_restore_the_query_span_as_current()
        {
            // Arrange — a query span is current, and a DB span is started under it
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == DarkerSemanticConventions.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(listener);
            using var tracer = new DarkerTracer();
            var querySpan = tracer.CreateQuerySpan(new SomeQuery(), options: InstrumentationOptions.QueryInformation);
            var dbSpan = tracer.CreateDbSpan(new DbSpanInfo(DbSystem.PostgreSql, "orders", "select"), querySpan);

            try
            {
                // Act
                tracer.EndSpan(dbSpan);

                // Assert — the query span is current again, not null
                Activity.Current.ShouldBeSameAs(querySpan);
            }
            finally
            {
                querySpan?.Stop();
                Activity.Current = null;
            }
        }
    }
}
