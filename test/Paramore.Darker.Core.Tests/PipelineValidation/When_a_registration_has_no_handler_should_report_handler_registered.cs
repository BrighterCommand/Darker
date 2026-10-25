using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class HandlerRegisteredRuleTests
    {
        private static readonly ValidationRuleId HandlerRegistered = new("HandlerRegistered");

        [Fact]
        public void When_a_registration_has_no_handler_should_report_handler_registered_on_the_sync_path()
        {
            // Arrange — a query registered on the sync registry with a null handler type
            var registry = new QueryHandlerRegistry();
            registry.Register(typeof(TestQuery), typeof(TestQuery.Result), null!);
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one error naming the query and the sync path
            var findings = result.Errors.Where(e => e.RuleId == HandlerRegistered).ToArray();
            findings.Length.ShouldBe(1);
            findings[0].Severity.ShouldBe(ValidationSeverity.Error);
            findings[0].Message.ShouldContain(nameof(TestQuery));
            findings[0].Message.ShouldContain("Sync");
        }

        [Fact]
        public void When_a_registration_has_no_handler_should_report_handler_registered_on_the_async_path()
        {
            // Arrange — a query registered on the async registry with a null handler type
            var registry = new QueryHandlerRegistryAsync();
            registry.Register(typeof(AsyncTestQuery), typeof(AsyncTestQuery.Result), null!);
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one error naming the query and the async path
            var findings = result.Errors.Where(e => e.RuleId == HandlerRegistered).ToArray();
            findings.Length.ShouldBe(1);
            findings[0].Severity.ShouldBe(ValidationSeverity.Error);
            findings[0].Message.ShouldContain(nameof(AsyncTestQuery));
            findings[0].Message.ShouldContain("Async");
        }

        [Fact]
        public void When_a_registration_has_no_handler_should_report_handler_registered_on_the_stream_path()
        {
            // Arrange — a query registered on the stream registry with a null handler type
            var registry = new StreamQueryHandlerRegistry();
            registry.Register(typeof(StreamTestQuery), typeof(string), null!);
            var pipelineValidator = new PipelineValidatorBuilder().WithStreamRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one error naming the query and the stream path
            var findings = result.Errors.Where(e => e.RuleId == HandlerRegistered).ToArray();
            findings.Length.ShouldBe(1);
            findings[0].Severity.ShouldBe(ValidationSeverity.Error);
            findings[0].Message.ShouldContain(nameof(StreamTestQuery));
            findings[0].Message.ShouldContain("Stream");
        }
    }
}
