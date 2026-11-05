using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class KeyedBesideUnkeyedScopeProviderTests
    {
        [Fact]
        public void When_a_keyed_scope_provider_sits_beside_an_unkeyed_one_should_report_nothing()
        {
            // Arrange — one unkeyed provider and a keyed provider of a different type
            var services = new ServiceCollection();
            services.AddSingleton<IAmAScopeProvider, TestScopeProvider>();
            services.AddKeyedSingleton<IAmAScopeProvider, KeyedTestScopeProvider>("k");
            services.AddDarker().ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — the keyed provider is not the one Darker resolves, so no DuplicateScopeProvider finding
            result.Warnings.Where(w => w.RuleId == new ValidationRuleId("DuplicateScopeProvider")).ShouldBeEmpty();
        }
    }
}
