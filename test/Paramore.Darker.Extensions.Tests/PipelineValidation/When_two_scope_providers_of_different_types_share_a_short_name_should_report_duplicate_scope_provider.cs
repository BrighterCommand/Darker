using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class SameShortNameScopeProvidersTests
    {
        [Fact]
        public void When_two_scope_providers_of_different_types_share_a_short_name_should_report_duplicate_scope_provider()
        {
            // Arrange — two different types, both named TestScopeProvider, in different namespaces
            var services = new ServiceCollection();
            services.AddSingleton<IAmAScopeProvider, TestDoubles.TestScopeProvider>();
            services.AddSingleton<IAmAScopeProvider, TestDoubles.Other.TestScopeProvider>();
            services.AddDarker().ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — they are different implementation types, so the duplicate is reported
            var finding = result.Warnings.Where(w => w.RuleId == new ValidationRuleId("DuplicateScopeProvider")).ShouldHaveSingleItem();
            finding.Severity.ShouldBe(ValidationSeverity.Warning);
        }
    }
}
