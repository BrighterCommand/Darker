using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class SnapshotRefreshTests
    {
        [Fact]
        public void When_a_scope_provider_is_added_between_two_ValidatePipelines_calls_should_be_seen()
        {
            // Arrange — JoinAmbient and Scoped, ValidatePipelines(), then a provider is added, then ValidatePipelines() again
            var services = new ServiceCollection();
            var builder = services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Scoped;
                    options.DefaultScopeAffinity = ScopeAffinity.JoinAmbient;
                })
                .ValidatePipelines();
            services.AddSingleton<IAmAScopeProvider, TestScopeProvider>();
            builder.ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — the last call's snapshot sees the provider, so no InertScopeAffinity is reported
            result.Errors.Concat(result.Warnings).Where(f => f.RuleId == new ValidationRuleId("InertScopeAffinity")).ShouldBeEmpty();
        }
    }
}
