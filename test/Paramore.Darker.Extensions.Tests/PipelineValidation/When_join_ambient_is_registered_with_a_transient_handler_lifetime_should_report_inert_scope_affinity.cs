using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class InertScopeAffinityTransientTests
    {
        [Fact]
        public void When_join_ambient_is_registered_with_a_transient_handler_lifetime_should_report_inert_scope_affinity()
        {
            // Arrange — a JoinAmbient provider with Transient handlers
            var services = new ServiceCollection();
            services.AddDarkerScopeProvider<TestScopeProvider>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Transient)
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — one InertScopeAffinity Error naming the provider and lifetime, with advice and the README link
            var finding = result.Errors.Where(e => e.RuleId == new ValidationRuleId("InertScopeAffinity")).ShouldHaveSingleItem();
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Source.ShouldBe("Scope affinity");
            finding.Message.ShouldContain(nameof(TestScopeProvider));
            finding.Message.ShouldContain("Transient");
            finding.Message.ShouldContain("ServiceLifetime.Scoped");
            finding.Message.ShouldContain("AlwaysNew");
            finding.Message.ShouldEndWith("README.md#inertscopeaffinity");
        }
    }
}
