using System.Linq;
using Paramore.Darker.Core.Tests.Exported;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class HandlerPublicRuleTests
    {
        private static readonly ValidationRuleId HandlerPublic = new("HandlerPublic");

        [Fact]
        public void When_an_internal_handler_is_registered_explicitly_should_report_handler_public()
        {
            // Arrange — an internal handler registered explicitly on the sync registry
            var registry = new QueryHandlerRegistry();
            registry.Register<StepOrderRecordingQuery, string, StepOrderRecordingQueryHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one error naming the handler and advising to make it public
            var findings = result.Errors.Where(e => e.RuleId == HandlerPublic).ToArray();
            findings.Length.ShouldBe(1);
            findings[0].Severity.ShouldBe(ValidationSeverity.Error);
            findings[0].Message.ShouldContain(nameof(StepOrderRecordingQueryHandler));
            findings[0].Message.ShouldContain("public");
        }

        [Fact]
        public void When_an_internal_handler_is_registered_explicitly_should_report_handler_public_for_a_public_nested_handler()
        {
            // Arrange — a public handler nested inside an internal type
            var registry = new QueryHandlerRegistry();
            registry.Register<TestQuery, TestQuery.Result, InternalHandlerHost.NestedPublicHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one error naming the nested handler
            var findings = result.Errors.Where(e => e.RuleId == HandlerPublic).ToArray();
            findings.Length.ShouldBe(1);
            findings[0].Message.ShouldContain(nameof(InternalHandlerHost.NestedPublicHandler));
        }

        [Fact]
        public void When_an_internal_handler_is_registered_explicitly_should_not_report_a_public_handler()
        {
            // Arrange — a public handler from Exported/
            var registry = new QueryHandlerRegistry();
            registry.Register<TestQueryA, System.Guid, TestQueryHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — no HandlerPublic finding
            result.Errors.Where(e => e.RuleId == HandlerPublic).ShouldBeEmpty();
        }
    }
}
