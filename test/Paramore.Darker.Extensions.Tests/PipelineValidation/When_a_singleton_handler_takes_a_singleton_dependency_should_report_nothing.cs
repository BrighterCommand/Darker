using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class SingletonDependencyTests
    {
        [Fact]
        public void When_a_singleton_handler_takes_a_singleton_dependency_should_report_nothing()
        {
            // Arrange — a Singleton HandlerLifetime and a handler taking a Singleton dependency
            var services = new ServiceCollection();
            services.AddSingleton<SingletonDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, SingletonDependencyHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert — no CaptiveDependency finding
            result.Errors.Concat(result.Warnings).ShouldNotContain(f => f.RuleId == new ValidationRuleId("CaptiveDependency"));
        }
    }
}
