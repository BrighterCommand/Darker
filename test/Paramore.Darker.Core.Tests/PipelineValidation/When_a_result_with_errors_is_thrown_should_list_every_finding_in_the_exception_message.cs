using System;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class PipelineValidationExceptionTests
    {
        [Fact]
        public void When_a_result_with_errors_is_thrown_should_list_every_finding_in_the_exception_message()
        {
            // Arrange — two errors and one warning
            var result = new PipelineValidationResult(
                [
                    new ValidationError(ValidationSeverity.Error, "Handler 'A'", "first error") { RuleId = new ValidationRuleId("RuleOne") },
                    new ValidationError(ValidationSeverity.Error, "Handler 'B'", "second error") { RuleId = new ValidationRuleId("RuleTwo") }
                ],
                [
                    new ValidationError(ValidationSeverity.Warning, "Handler 'C'", "a warning") { RuleId = new ValidationRuleId("RuleThree") }
                ]);

            // Act
            var exception = Should.Throw<PipelineValidationException>(() => result.ThrowIfInvalid());

            // Assert — the count, then errors first, then warnings, one line each
            var lines = exception.Message.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
            lines[0].ShouldContain("2 error");
            lines[1..].ShouldBe(
            [
                "[RuleOne] Handler 'A': first error",
                "[RuleTwo] Handler 'B': second error",
                "[RuleThree] Handler 'C': a warning"
            ]);
            exception.Result.ShouldBeSameAs(result);
        }

        [Fact]
        public void When_a_result_has_only_warnings_should_not_throw_from_ThrowIfInvalid()
        {
            // Arrange
            var result = new PipelineValidationResult(
                [],
                [new ValidationError(ValidationSeverity.Warning, "Handler 'C'", "a warning")]);

            // Act / Assert
            Should.NotThrow(() => result.ThrowIfInvalid());
        }

        [Fact]
        public void When_PipelineValidationException_is_declared_should_derive_from_Exception_and_not_ConfigurationException()
        {
            // Assert
            typeof(PipelineValidationException).BaseType.ShouldBe(typeof(Exception));
        }
    }
}
