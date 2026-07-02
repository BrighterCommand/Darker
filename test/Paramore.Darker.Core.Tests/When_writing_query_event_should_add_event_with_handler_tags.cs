using System.Diagnostics;
using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    [Collection("DarkerActivitySource")]
    public class When_writing_query_event_should_add_event_with_handler_tags
    {
        [Fact]
        public void Should_add_event_with_async_handler_tags_when_isAsync_is_true_and_isSink_is_true()
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
            var span = tracer.CreateQuerySpan(query, options: InstrumentationOptions.QueryInformation);

            // Act
            DarkerTracer.WriteQueryEvent(span, "MyHandler", isAsync: true, InstrumentationOptions.QueryInformation, isSink: true);

            // Assert
            try
            {
                span.ShouldNotBeNull();
                var events = span.Events.ToList();
                events.Count.ShouldBe(1);
                var ev = events[0];
                ev.Name.ShouldBe("MyHandler");
                var tags = ev.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                tags[DarkerSemanticConventions.HandlerName].ShouldBe("MyHandler");
                tags[DarkerSemanticConventions.HandlerType].ShouldBe("async");
                tags[DarkerSemanticConventions.IsSink].ShouldBe(true);
            }
            finally
            {
                span?.Stop();
            }
        }

        [Fact]
        public void Should_emit_sync_handlertype_when_isAsync_is_false_and_isSink_is_false()
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
            var span = tracer.CreateQuerySpan(query, options: InstrumentationOptions.QueryInformation);

            // Act
            DarkerTracer.WriteQueryEvent(span, "MyHandler", isAsync: false, InstrumentationOptions.QueryInformation, isSink: false);

            // Assert
            try
            {
                span.ShouldNotBeNull();
                var ev = span.Events.Single(e => e.Name == "MyHandler");
                var tags = ev.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                tags[DarkerSemanticConventions.HandlerType].ShouldBe("sync");
                tags[DarkerSemanticConventions.IsSink].ShouldBe(false);
            }
            finally
            {
                span?.Stop();
            }
        }

        [Fact]
        public void Should_be_no_op_when_span_is_null()
        {
            // Arrange / Act / Assert — null span must not throw
            Should.NotThrow(() => DarkerTracer.WriteQueryEvent(null, "X", false, InstrumentationOptions.QueryInformation));
        }
    }
}
