using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class AddDarkerScopeProviderReplacesTests
    {
        [Fact]
        public void When_AddDarkerScopeProvider_replaces_a_provider_should_report_no_duplicate_scope_provider()
        {
            // Arrange — a provider registered through the Darker API, then replaced by a different type through the same API
            var services = new ServiceCollection();
            services.AddDarkerScopeProvider<TestScopeProvider>();
            services.AddDarkerScopeProvider<KeyedTestScopeProvider>();
            services.AddDarker().ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — the Darker API alone never produces this finding
            result.Warnings.Where(w => w.RuleId == new ValidationRuleId("DuplicateScopeProvider")).ShouldBeEmpty();
        }
    }
}
