using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class InertScopeAffinityRegisteredOverrideTests
    {
        [Fact]
        public void When_a_registered_always_new_overrides_a_join_ambient_default_should_report_nothing()
        {
            // Arrange — JoinAmbient as the option default, Transient handlers, and a provider registered with AlwaysNew
            var services = new ServiceCollection();
            services.AddDarkerScopeProvider<TestScopeProvider>(ScopeAffinity.AlwaysNew);
            services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Transient;
                    options.DefaultScopeAffinity = ScopeAffinity.JoinAmbient;
                })
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — the registration wins over the option, so there is no InertScopeAffinity finding
            result.Errors.Where(e => e.RuleId == new ValidationRuleId("InertScopeAffinity")).ShouldBeEmpty();
        }
    }
}
