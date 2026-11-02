using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class ComponentRegisteredKeyedTests
    {
        [Fact]
        public void When_a_component_is_registered_keyed_should_report_nothing()
        {
            // Arrange — a Transient HandlerLifetime, and the handler also registered keyed Singleton before AddDarker
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            services.AddKeyedSingleton<CaptiveHandler>("k");
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Transient)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, CaptiveHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert — Darker resolves the handler unkeyed, so the keyed Singleton record is not examined
            result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ShouldBeEmpty();
        }
    }
}
