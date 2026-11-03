using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class ParameterlessConstructorTests
    {
        [Fact]
        public void When_a_handler_has_only_a_parameterless_constructor_should_yield_no_captive_finding()
        {
            // Arrange — a Singleton HandlerLifetime and a handler with no constructor parameters
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, ParameterlessConstructorHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert
            result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ShouldBeEmpty();
        }
    }
}
