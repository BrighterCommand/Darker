using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class InertScopeAffinityBothCausesTests
    {
        [Fact]
        public void When_join_ambient_has_both_causes_should_report_one_finding_naming_both()
        {
            // Arrange — JoinAmbient as the option default, Transient handlers, and no IAmAScopeProvider registered
            var services = new ServiceCollection();
            services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Transient;
                    options.DefaultScopeAffinity = ScopeAffinity.JoinAmbient;
                })
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — one InertScopeAffinity Error whose message names both the Transient and the missing-provider advice
            var finding = result.Errors.Where(e => e.RuleId == new ValidationRuleId("InertScopeAffinity")).ShouldHaveSingleItem();
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain("HandlerLifetime is Transient");
            finding.Message.ShouldContain("Set ServiceLifetime.Scoped");
            finding.Message.ShouldContain("no IAmAScopeProvider is registered");
            finding.Message.ShouldContain("AddDarkerScopeProvider");
        }
    }
}
