using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class DuplicateScopeProviderTests
    {
        [Fact]
        public void When_two_different_scope_providers_are_registered_should_report_duplicate_scope_provider()
        {
            // Arrange — two different IAmAScopeProvider types registered with AddSingleton
            var services = new ServiceCollection();
            services.AddSingleton<IAmAScopeProvider, TestScopeProvider>();
            services.AddSingleton<IAmAScopeProvider, AsyncLocalScopeProvider>();
            services.AddDarker().ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — one Warning names both types, marks the second as used, and advises keeping one
            var finding = result.Warnings.Where(w => w.RuleId == new ValidationRuleId("DuplicateScopeProvider")).ShouldHaveSingleItem();
            finding.Severity.ShouldBe(ValidationSeverity.Warning);
            finding.Source.ShouldBe("Scope provider registration");
            finding.Message.ShouldContain("TestScopeProvider");
            finding.Message.ShouldContain("AsyncLocalScopeProvider");
            finding.Message.IndexOf("AsyncLocalScopeProvider").ShouldBeGreaterThan(finding.Message.IndexOf("TestScopeProvider"));
            finding.Message.ShouldContain("AsyncLocalScopeProvider is used");
            finding.Message.ShouldContain("AddDarkerScopeProvider");
            finding.Message.ShouldContain("AddDarkerRequestScope");
            finding.Message.ShouldEndWith("README.md#duplicatescopeprovider");
        }
    }
}
