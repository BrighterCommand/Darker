using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class InertScopeAffinityScopedJoinAmbientTests
    {
        [Fact]
        public void When_join_ambient_has_a_scoped_lifetime_and_a_provider_should_report_nothing()
        {
            // Arrange — JoinAmbient registered with a provider, and Scoped handlers
            var services = new ServiceCollection();
            services.AddDarkerScopeProvider<TestScopeProvider>(ScopeAffinity.JoinAmbient);
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — JoinAmbient can take effect, so there is no InertScopeAffinity finding
            result.Errors.Where(e => e.RuleId == new ValidationRuleId("InertScopeAffinity")).ShouldBeEmpty();
        }
    }
}
