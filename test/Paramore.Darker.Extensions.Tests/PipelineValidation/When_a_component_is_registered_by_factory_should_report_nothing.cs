using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class ComponentFactoryRegistrationTests
    {
        [Fact]
        public void When_a_component_is_registered_by_factory_should_report_nothing()
        {
            // Arrange — a Singleton handler registered by a factory before AddDarker, with a Transient HandlerLifetime; Darker's TryAdd adds nothing
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            services.AddSingleton(sp => new CaptiveHandler(sp.GetRequiredService<ScopedDependency>()));
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Transient)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, CaptiveHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container, without resolving the handler
            var result = provider.ValidateAll();

            // Assert — a factory record has no implementation type, so no CaptiveDependency finding is reported
            result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ShouldBeEmpty();
        }
    }
}
