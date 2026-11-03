using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class TransientDependencyTests
    {
        [Fact]
        public void When_a_singleton_handler_takes_a_transient_dependency_should_report_nothing()
        {
            // Arrange — a Singleton HandlerLifetime and a handler taking a Transient dependency
            var services = new ServiceCollection();
            services.AddTransient<TransientDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, TransientDependencyHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert — no CaptiveDependency finding
            result.Errors.Concat(result.Warnings).ShouldNotContain(f => f.RuleId == new ValidationRuleId("CaptiveDependency"));
        }
    }
}
