using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class ThrowingInspectorValidationTests
    {
        [Fact]
        public void When_a_registry_inspector_throws_should_report_rule_evaluation_failed_and_validate_the_others()
        {
            // Arrange — the sync and async inspectors both throw
            var validator = new PipelineValidatorBuilder()
                .WithSyncRegistry(new ThrowingInspectorQueryHandlerRegistry())
                .WithAsyncRegistry(new ThrowingInspectorQueryHandlerRegistryAsync())
                .Build();

            // Act
            PipelineValidationResult? result = null;
            Should.NotThrow(() => result = validator.Validate());

            // Assert — one error per registry, sync first; the second shows validation went on
            result!.Errors.Count.ShouldBe(2);
            result.Errors.ShouldAllBe(e => e.RuleId == ValidationRuleIds.EvaluationFailed);
            result.Errors.ShouldAllBe(e => e.Severity == ValidationSeverity.Error);

            result.Errors[0].Source.ShouldContain(nameof(ThrowingInspectorQueryHandlerRegistry));
            result.Errors[0].Message.ShouldContain(ThrowingInspectorQueryHandlerRegistry.FAULT_MESSAGE);
            result.Errors[1].Source.ShouldContain(nameof(ThrowingInspectorQueryHandlerRegistryAsync));
            result.Errors[1].Message.ShouldContain(ThrowingInspectorQueryHandlerRegistryAsync.FAULT_MESSAGE);
        }
    }
}
