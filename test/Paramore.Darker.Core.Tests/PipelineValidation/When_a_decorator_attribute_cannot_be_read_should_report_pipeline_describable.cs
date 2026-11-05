using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class UnreadableAttributePipelineDescribableRuleTests
    {
        private static readonly ValidationRuleId PipelineDescribable = new("PipelineDescribable");

        [Fact]
        public void When_a_decorator_attribute_cannot_be_read_should_report_pipeline_describable()
        {
            // Arrange — an async handler whose ExecuteAsync carries an attribute whose constructor throws
            var registry = new QueryHandlerRegistryAsync();
            registry.Register<AsyncTestQuery, AsyncTestQuery.Result, UnreadableAttributeAsyncHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            PipelineValidationResult? result = null;
            var exception = Record.Exception(() => result = pipelineValidator.Validate());

            // Assert — no exception; the error names the handler and the method and carries the exception message
            exception.ShouldBeNull();
            var finding = result!.Errors.Single(e => e.RuleId == PipelineDescribable);
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain(nameof(UnreadableAttributeAsyncHandler));
            finding.Message.ShouldContain("ExecuteAsync");
            finding.Message.ShouldContain(ThrowingConstructorAttribute.FaultMessage);
        }
    }
}
