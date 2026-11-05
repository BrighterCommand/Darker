using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class InertScopeAffinitySingletonDefaultTests
    {
        [Fact]
        public void When_the_default_affinity_is_join_ambient_with_a_singleton_handler_lifetime_should_report_inert_scope_affinity()
        {
            // Arrange — JoinAmbient as the option default, Singleton handlers, and a directly registered provider
            var services = new ServiceCollection();
            services.AddSingleton<IAmAScopeProvider, TestScopeProvider>();
            services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Singleton;
                    options.DefaultScopeAffinity = ScopeAffinity.JoinAmbient;
                })
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — one InertScopeAffinity Error naming the option and lifetime, with the README link
            var finding = result.Errors.Where(e => e.RuleId == new ValidationRuleId("InertScopeAffinity")).ShouldHaveSingleItem();
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain("DarkerOptions.DefaultScopeAffinity");
            finding.Message.ShouldContain("Singleton");
            finding.Message.ShouldEndWith("README.md#inertscopeaffinity");
        }
    }
}
