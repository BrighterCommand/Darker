using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class InertScopeAffinityKeyedProviderTests
    {
        [Fact]
        public void When_the_only_scope_provider_is_keyed_should_report_inert_scope_affinity()
        {
            // Arrange — JoinAmbient, Scoped handlers, and the only IAmAScopeProvider registered under a key
            var services = new ServiceCollection();
            services.AddKeyedSingleton<IAmAScopeProvider, TestScopeProvider>("k");
            services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Scoped;
                    options.DefaultScopeAffinity = ScopeAffinity.JoinAmbient;
                })
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = Should.NotThrow(() => provider.ValidateAll());

            // Assert — the keyed provider does not count, so the InertScopeAffinity Error is reported
            var finding = result.Errors.Where(e => e.RuleId == new ValidationRuleId("InertScopeAffinity")).ShouldHaveSingleItem();
            finding.Severity.ShouldBe(ValidationSeverity.Error);
        }
    }

    public class InertScopeAffinityUnkeyedProviderTests
    {
        [Fact]
        public void When_an_unkeyed_provider_is_added_with_AddSingleton_should_report_nothing()
        {
            // Arrange — JoinAmbient, Scoped handlers, and an unkeyed IAmAScopeProvider
            var services = new ServiceCollection();
            services.AddSingleton<IAmAScopeProvider, TestScopeProvider>();
            services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Scoped;
                    options.DefaultScopeAffinity = ScopeAffinity.JoinAmbient;
                })
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — the rule is satisfied
            result.Errors.Where(e => e.RuleId == new ValidationRuleId("InertScopeAffinity")).ShouldBeEmpty();
        }
    }
}
