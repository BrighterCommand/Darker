using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class PipelineDescribableRuleTests
    {
        private static readonly ValidationRuleId PipelineDescribable = new("PipelineDescribable");

        [Fact]
        public void When_a_handler_has_overloaded_execute_async_should_report_pipeline_describable()
        {
            // Arrange — an async handler with two public ExecuteAsync overloads
            var registry = new QueryHandlerRegistryAsync();
            registry.Register<AsyncTestQuery, AsyncTestQuery.Result, OverloadedExecuteAsyncHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();
            var ambiguousMessage = Record.Exception(() => typeof(OverloadedExecuteAsyncHandler).GetMethod("ExecuteAsync"))!.Message;

            // Act
            PipelineValidationResult? result = null;
            var exception = Record.Exception(() => result = pipelineValidator.Validate());

            // Assert — no exception; the error names the handler and the method and carries the reflection message
            exception.ShouldBeNull();
            var finding = result!.Errors.Single(e => e.RuleId == PipelineDescribable);
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain(nameof(OverloadedExecuteAsyncHandler));
            finding.Message.ShouldContain("ExecuteAsync");
            finding.Message.ShouldContain(ambiguousMessage);
        }
    }
}
