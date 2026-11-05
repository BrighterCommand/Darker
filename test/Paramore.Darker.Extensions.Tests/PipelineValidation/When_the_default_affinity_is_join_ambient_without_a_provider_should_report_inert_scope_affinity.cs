using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class InertScopeAffinityMissingProviderTests
    {
        [Fact]
        public void When_the_default_affinity_is_join_ambient_without_a_provider_should_report_inert_scope_affinity()
        {
            // Arrange — JoinAmbient as the option default, Scoped handlers, and no IAmAScopeProvider registered
            var services = new ServiceCollection();
            services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Scoped;
                    options.DefaultScopeAffinity = ScopeAffinity.JoinAmbient;
                })
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — one InertScopeAffinity Error naming the option, the missing provider and the advice, with the README link
            var finding = result.Errors.Where(e => e.RuleId == new ValidationRuleId("InertScopeAffinity")).ShouldHaveSingleItem();
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Source.ShouldBe("Scope affinity");
            finding.Message.ShouldContain("DarkerOptions.DefaultScopeAffinity");
            finding.Message.ShouldContain("no IAmAScopeProvider is registered");
            finding.Message.ShouldContain("AddDarkerScopeProvider");
            finding.Message.ShouldContain("AddDarkerRequestScope()");
            finding.Message.ShouldEndWith("README.md#inertscopeaffinity");
        }
    }
}
