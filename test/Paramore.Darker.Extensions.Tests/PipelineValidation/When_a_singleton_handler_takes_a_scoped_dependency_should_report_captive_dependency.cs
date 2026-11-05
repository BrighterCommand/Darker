using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class CaptiveDependencyTests
    {
        [Fact]
        public void When_a_singleton_handler_takes_a_scoped_dependency_should_report_captive_dependency()
        {
            // Arrange — a Singleton HandlerLifetime, a handler taking a Scoped dependency, and a plain Singleton service taking one too
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            services.AddSingleton<PlainService>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, CaptiveHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container, without resolving the handler
            var result = provider.ValidateAll();

            // Assert — one Warning names the handler, why it is Singleton, the parameter and the fix; PlainService is not reported
            var captive = result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ToList();
            var finding = captive.ShouldHaveSingleItem();
            finding.Severity.ShouldBe(ValidationSeverity.Warning);
            finding.Source.ShouldBe("Handler 'CaptiveHandler'");
            finding.Message.ShouldContain("HandlerLifetime");
            finding.Message.ShouldContain("dependency");
            finding.Message.ShouldContain("ScopedDependency");
            finding.Message.ShouldContain("Singleton or Transient");
            finding.Message.ShouldEndWith("README.md#captivedependency");
            captive.ShouldNotContain(w => w.Source.Contains("PlainService") || w.Message.Contains("PlainService"));
        }
    }
}
