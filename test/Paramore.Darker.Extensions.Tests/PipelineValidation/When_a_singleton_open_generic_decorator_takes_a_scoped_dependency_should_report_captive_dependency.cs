using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class OpenGenericDecoratorCaptiveDependencyTests
    {
        [Fact]
        public void When_a_singleton_open_generic_decorator_takes_a_scoped_dependency_should_report_captive_dependency()
        {
            // Arrange — a Singleton HandlerLifetime and an open-generic decorator taking a Scoped dependency
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            var builder = services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton);
            builder.RegisterDecorator(typeof(CaptiveDecoratorAsync<,>));
            builder.ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container, without resolving the decorator
            var result = provider.ValidateAll();

            // Assert — one Warning names the decorator in open generic notation, why it is Singleton and the parameter
            var captive = result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ToList();
            var finding = captive.ShouldHaveSingleItem();
            finding.Severity.ShouldBe(ValidationSeverity.Warning);
            finding.Source.ShouldBe("Decorator 'CaptiveDecoratorAsync<,>'");
            finding.Message.ShouldStartWith("CaptiveDecoratorAsync<,> counts as Singleton");
            finding.Message.ShouldContain("HandlerLifetime is Singleton");
            finding.Message.ShouldContain("ScopedDependency");
        }
    }
}
