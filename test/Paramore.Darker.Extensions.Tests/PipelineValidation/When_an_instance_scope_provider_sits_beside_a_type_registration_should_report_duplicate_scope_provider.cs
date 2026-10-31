using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class DuplicateScopeProviderInstanceTests
    {
        [Fact]
        public void When_an_instance_scope_provider_sits_beside_a_type_registration_should_report_duplicate_scope_provider()
        {
            // Arrange — an instance of one type, then a type registration of another
            var services = new ServiceCollection();
            services.AddSingleton<IAmAScopeProvider>(new TestScopeProvider());
            services.AddSingleton<IAmAScopeProvider, AsyncLocalScopeProvider>();
            services.AddDarker().ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();
            var finding = result.Warnings
                .Where(w => w.RuleId == new ValidationRuleId("DuplicateScopeProvider"))
                .ShouldHaveSingleItem();

            // Assert — the instance is named by its runtime type, not by the service type
            finding.Severity.ShouldBe(ValidationSeverity.Warning);
            finding.Message.ShouldContain("More than one IAmAScopeProvider is registered: TestScopeProvider, AsyncLocalScopeProvider.");
            finding.Message.ShouldContain("AsyncLocalScopeProvider is used");
        }
    }
}
