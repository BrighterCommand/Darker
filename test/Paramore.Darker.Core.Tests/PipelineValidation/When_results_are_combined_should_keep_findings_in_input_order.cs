using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class CombineValidationResultsTests
    {
        private static ValidationError Error(string message) => new(ValidationSeverity.Error, "source", message);
        private static ValidationError Warning(string message) => new(ValidationSeverity.Warning, "source", message);

        [Fact]
        public void When_results_are_combined_should_keep_findings_in_input_order()
        {
            // Arrange
            var first = new PipelineValidationResult([Error("e1"), Error("e2")], [Warning("w1")]);
            var second = new PipelineValidationResult([Error("e3")], [Warning("w2"), Warning("w3")]);

            // Act
            var combined = PipelineValidationResult.Combine(first, second);

            // Assert
            combined.Errors.ShouldBe([first.Errors[0], first.Errors[1], second.Errors[0]]);
            combined.Warnings.ShouldBe([first.Warnings[0], second.Warnings[0], second.Warnings[1]]);
        }

        [Fact]
        public void When_any_combined_result_has_an_error_should_not_be_valid()
        {
            // Arrange
            var clean = new PipelineValidationResult([], [Warning("w1")]);
            var failing = new PipelineValidationResult([Error("e1")], []);

            // Act
            var combined = PipelineValidationResult.Combine(clean, failing);

            // Assert
            combined.IsValid.ShouldBeFalse();
        }
    }
}
