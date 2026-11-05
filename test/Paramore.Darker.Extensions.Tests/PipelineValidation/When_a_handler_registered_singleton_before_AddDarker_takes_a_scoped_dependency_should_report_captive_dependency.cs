using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class CaptiveDependencyOwnSingletonRegistrationTests
    {
        [Fact]
        public void When_a_handler_registered_singleton_before_AddDarker_takes_a_scoped_dependency_should_report_captive_dependency()
        {
            // Arrange — a Transient HandlerLifetime, but the handler is registered Singleton before AddDarker, so TryAdd keeps that descriptor
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            services.AddSingleton<CaptiveHandler>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Transient)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, CaptiveHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert — one Warning, saying the handler counts as Singleton because of its own registration
            var captive = result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ToList();
            var finding = captive.ShouldHaveSingleItem();
            finding.Source.ShouldBe("Handler 'CaptiveHandler'");
            finding.Message.ShouldContain("its own registration is Singleton");
            finding.Message.ShouldContain("ScopedDependency");
        }
    }
}
