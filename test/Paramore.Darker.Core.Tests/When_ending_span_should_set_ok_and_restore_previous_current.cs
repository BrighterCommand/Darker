using System;
using System.Diagnostics;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    [Collection("DarkerActivitySource")]
    public class When_ending_span_should_set_ok_and_restore_previous_current
    {
        private static ActivityListener CreateListener() => new ActivityListener
        {
            ShouldListenTo = s => s.Name == "paramore.darker",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
        };

        [Fact]
        public void Should_restore_Activity_Current_to_prior_value_after_EndSpan()
        {
            // Arrange — start an ambient activity to act as the previous Activity.Current
            using var listener = CreateListener();
            ActivitySource.AddActivityListener(listener);
            using var tracer = new DarkerTracer();
            var query = new SomeQuery();

            using var previousActivity = new Activity("previous.ambient").Start();
            var capturedPrior = Activity.Current; // == previousActivity

            // Act — CreateQuerySpan sets Activity.Current = span; EndSpan should restore it
            var span = tracer.CreateQuerySpan(query, options: InstrumentationOptions.QueryInformation);
            tracer.EndSpan(span);

            // Assert — Activity.Current is back to what it was before CreateQuerySpan
            Activity.Current.ShouldBe(capturedPrior);

            previousActivity.Stop();
        }

        [Fact]
        public void Should_set_status_to_ok_when_span_has_no_explicit_status()
        {
            // Arrange
            using var listener = CreateListener();
            ActivitySource.AddActivityListener(listener);
            using var tracer = new DarkerTracer();
            var query = new SomeQuery();
            var span = tracer.CreateQuerySpan(query, options: InstrumentationOptions.QueryInformation);

            // Act
            tracer.EndSpan(span);

            // Assert — a span with Unset status should be promoted to Ok
            span.ShouldNotBeNull();
            span.Status.ShouldBe(ActivityStatusCode.Ok);
        }

        [Fact]
        public void Should_not_overwrite_error_status_when_span_already_has_error_set()
        {
            // Arrange
            using var listener = CreateListener();
            ActivitySource.AddActivityListener(listener);
            using var tracer = new DarkerTracer();
            var query = new SomeQuery();
            var exception = new InvalidOperationException("something failed");
            var span = tracer.CreateQuerySpan(query, options: InstrumentationOptions.QueryInformation);
            tracer.AddExceptionToSpan(span, exception);

            // Act
            tracer.EndSpan(span);

            // Assert — Error status must not be overwritten to Ok
            span.ShouldNotBeNull();
            span.Status.ShouldBe(ActivityStatusCode.Error);
        }

        [Fact]
        public void Should_be_no_op_when_span_is_null()
        {
            // Arrange
            using var tracer = new DarkerTracer();

            // Act / Assert — a null span must not throw
            Should.NotThrow(() => tracer.EndSpan(null));
        }
    }
}
