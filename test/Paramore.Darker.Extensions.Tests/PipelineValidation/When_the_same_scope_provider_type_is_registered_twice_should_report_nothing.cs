using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class SameScopeProviderTypeTwiceTests
    {
        [Fact]
        public void When_the_same_scope_provider_type_is_registered_twice_should_report_nothing()
        {
            // Arrange — the same IAmAScopeProvider type registered twice
            var services = new ServiceCollection();
            services.AddSingleton<IAmAScopeProvider, TestScopeProvider>();
            services.AddSingleton<IAmAScopeProvider, TestScopeProvider>();
            services.AddDarker().ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — one distinct implementation, so no DuplicateScopeProvider finding
            result.Warnings.Where(w => w.RuleId == new ValidationRuleId("DuplicateScopeProvider")).ShouldBeEmpty();
        }
    }
}
