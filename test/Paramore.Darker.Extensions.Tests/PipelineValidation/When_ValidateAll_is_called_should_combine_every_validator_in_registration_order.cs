using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class ValidateAllTests
    {
        [Fact]
        public void When_ValidateAll_is_called_should_combine_every_validator_in_registration_order()
        {
            // Arrange — the pipeline validator first, then a second validator with a known Error and Warning
            var fixedError = new ValidationError(ValidationSeverity.Error, "FixedSource", "fixed error message")
            {
                RuleId = new ValidationRuleId("FixedError")
            };
            var fixedWarning = new ValidationError(ValidationSeverity.Warning, "FixedSource", "fixed warning message")
            {
                RuleId = new ValidationRuleId("FixedWarning")
            };
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddHandlers(r => r.Register<DuplicateStepQuery, DuplicateStepResult, DuplicateStepQueryHandler>())
                .ValidatePipelines();
            services.AddSingleton<IAmAPipelineValidator>(
                new FixedResultValidator(new PipelineValidationResult([fixedError], [fixedWarning])));
            using var provider = services.BuildServiceProvider();

            // Act — one combined view, without throwing
            var result = provider.ValidateAll();

            // Assert — invalid, the fixed Error is present, and the Warnings keep registration order
            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(fixedError);
            var duplicateStepIndex = IndexOfRule(result, "DuplicateStep");
            var fixedWarningIndex = result.Warnings.ToList().IndexOf(fixedWarning);
            duplicateStepIndex.ShouldBeGreaterThanOrEqualTo(0);
            fixedWarningIndex.ShouldBeGreaterThan(duplicateStepIndex);
        }

        private static int IndexOfRule(PipelineValidationResult result, string ruleName) =>
            result.Warnings.ToList().FindIndex(w => w.RuleId == new ValidationRuleId(ruleName));
    }
}
