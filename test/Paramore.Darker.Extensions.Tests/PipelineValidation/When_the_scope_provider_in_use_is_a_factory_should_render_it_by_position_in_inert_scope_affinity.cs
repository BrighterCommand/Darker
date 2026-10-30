using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class InertScopeAffinityFactoryProviderTests
    {
        [Fact]
        public void When_the_scope_provider_in_use_is_a_factory_should_render_it_by_position_in_inert_scope_affinity()
        {
            // Arrange — JoinAmbient registered, then a factory provider added last, with Transient handlers
            var services = new ServiceCollection();
            services.AddDarkerScopeProvider<TestScopeProvider>();
            services.AddSingleton<IAmAScopeProvider>(sp => new TestScopeProvider());
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Transient)
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — the provider in use is rendered as a factory registration at its 1-based position
            var finding = result.Errors.Where(e => e.RuleId == new ValidationRuleId("InertScopeAffinity")).ShouldHaveSingleItem();
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain("a factory registration at position 2");
            finding.Message.ShouldContain("Transient");
            finding.Message.ShouldEndWith("README.md#inertscopeaffinity");
        }
    }
}
