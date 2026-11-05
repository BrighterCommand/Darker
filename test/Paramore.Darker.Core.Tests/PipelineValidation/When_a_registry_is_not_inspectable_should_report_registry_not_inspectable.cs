using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class RegistryNotInspectableTests
    {
        private static readonly ValidationRuleId RegistryNotInspectable = new("RegistryNotInspectable");

        [Fact]
        public void When_a_registry_is_not_inspectable_should_report_registry_not_inspectable()
        {
            // Arrange
            var validator = new PipelineValidatorBuilder()
                .WithSyncRegistry(new NonInspectableQueryHandlerRegistry())
                .Build();

            // Act
            var result = validator.Validate();

            // Assert
            var warning = result.Warnings.Single(w => w.RuleId == RegistryNotInspectable);
            warning.Severity.ShouldBe(ValidationSeverity.Warning);
            warning.Message.ShouldContain(nameof(NonInspectableQueryHandlerRegistry));
            result.Errors.ShouldBeEmpty();
        }
    }
}
