using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class DecoratorTypePipelineDescribableRuleTests
    {
        private static readonly ValidationRuleId PipelineDescribable = new("PipelineDescribable");

        [Fact]
        public void When_a_decorator_type_is_null_or_throws_should_report_pipeline_describable_for_a_null_type()
        {
            // Arrange — an async handler whose attribute returns a null decorator type
            var registry = new QueryHandlerRegistryAsync();
            registry.Register<AsyncTestQuery, AsyncTestQuery.Result, NullDecoratorTypeAsyncHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            PipelineValidationResult? result = null;
            var exception = Record.Exception(() => result = pipelineValidator.Validate());

            // Assert — no exception; the error names the attribute
            exception.ShouldBeNull();
            var finding = result!.Errors.Single(e => e.RuleId == PipelineDescribable);
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain(nameof(NullDecoratorTypeAttribute));
        }

        [Fact]
        public void When_a_decorator_type_is_null_or_throws_should_report_pipeline_describable_for_a_throwing_type()
        {
            // Arrange — an async handler whose attribute throws from GetDecoratorType
            var registry = new QueryHandlerRegistryAsync();
            registry.Register<AsyncTestQuery, AsyncTestQuery.Result, ThrowingDecoratorTypeAsyncHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            PipelineValidationResult? result = null;
            var exception = Record.Exception(() => result = pipelineValidator.Validate());

            // Assert — no exception; the error names the attribute and carries the exception message
            exception.ShouldBeNull();
            var finding = result!.Errors.Single(e => e.RuleId == PipelineDescribable);
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain(nameof(ThrowingDecoratorTypeAttribute));
            finding.Message.ShouldContain(ThrowingDecoratorTypeAttribute.FaultMessage);
        }
    }
}
