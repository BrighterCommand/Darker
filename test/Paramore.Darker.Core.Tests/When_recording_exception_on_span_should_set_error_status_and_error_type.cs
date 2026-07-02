using System;
using System.Diagnostics;
using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    [Collection("DarkerActivitySource")]
    public class When_recording_exception_on_span_should_set_error_status_and_error_type
    {
        [Fact]
        public void Should_set_error_status_record_exception_event_and_tag_error_type()
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
            var exception = new InvalidOperationException("something went wrong");

            // Body-free options: AddExceptionToSpan does not serialise the query body, so
            // QueryInformation is sufficient and avoids locking the process-global JSON static.
            var span = tracer.CreateQuerySpan(query, options: InstrumentationOptions.QueryInformation);

            // Act
            tracer.AddExceptionToSpan(span, exception);

            // Assert
            try
            {
                span.ShouldNotBeNull();

                span.Status.ShouldBe(ActivityStatusCode.Error);

                span.Events.Any(e => e.Name == "exception").ShouldBeTrue();

                span.GetTagItem("error.type").ShouldBe(typeof(InvalidOperationException).Name);
            }
            finally
            {
                span?.Stop();
            }
        }

        [Fact]
        public void Should_be_no_op_when_span_is_null()
        {
            // Arrange
            using var tracer = new DarkerTracer();
            var exception = new Exception("test");

            // Act / Assert — null span must not throw
            Should.NotThrow(() => tracer.AddExceptionToSpan(null, exception));
        }
    }
}
