using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class LaterDescriptorReplacesSingletonComponentTests
    {
        [Fact]
        public void When_a_later_descriptor_replaces_a_singleton_component_should_report_nothing()
        {
            // Arrange — the handler is registered Singleton and then Transient before AddDarker, with a Transient HandlerLifetime
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            services.AddSingleton<CaptiveHandler>();
            services.AddTransient<CaptiveHandler>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Transient)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, CaptiveHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container, without resolving the handler
            var result = provider.ValidateAll();

            // Assert — only the effective (Transient) descriptor counts, so nothing is captive
            result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ShouldBeEmpty();
        }
    }
}
