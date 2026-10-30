using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.AspNetCore.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class RequestScopeTransientInertScopeAffinityTests
    {
        [Fact]
        public void When_the_request_scope_is_registered_with_a_transient_handler_lifetime_should_report_inert_scope_affinity()
        {
            // Arrange - the request scope registered before AddDarker, with Transient handlers
            var services = new ServiceCollection();
            services.AddDarkerRequestScope();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Transient)
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act - validate through the real container
            var result = provider.ValidateAll();

            // Assert - one InertScopeAffinity Error naming the provider in use and Transient
            var finding = result.Errors.Where(e => e.RuleId == new ValidationRuleId("InertScopeAffinity")).ShouldHaveSingleItem();
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain("HttpContextScopeProvider");
            finding.Message.ShouldContain("Transient");
        }
    }
}
