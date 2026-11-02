using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class ComponentInstanceRegistrationTests
    {
        [Fact]
        public void When_a_component_is_registered_as_an_instance_should_report_nothing()
        {
            // Arrange — a Singleton handler instance registered before AddDarker, with a Transient HandlerLifetime; Darker's TryAdd adds nothing
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            services.AddSingleton(new CaptiveHandler(new ScopedDependency()));
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Transient)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, CaptiveHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container, without resolving the handler
            var result = provider.ValidateAll();

            // Assert — an instance record has no implementation type, so no CaptiveDependency finding is reported
            result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ShouldBeEmpty();
        }
    }
}
