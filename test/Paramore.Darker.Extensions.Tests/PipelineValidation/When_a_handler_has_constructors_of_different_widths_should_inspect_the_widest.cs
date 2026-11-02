using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class ConstructorWidthTests
    {
        [Fact]
        public void When_a_handler_has_constructors_of_different_widths_should_inspect_the_widest()
        {
            // Arrange — a Singleton HandlerLifetime and a handler whose wider constructor (declared second) takes a Scoped dependency
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            services.AddSingleton<OtherDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, WideAndNarrowConstructorHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert — one Warning names the dependency parameter of the widest constructor
            var captive = result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ToList();
            var finding = captive.ShouldHaveSingleItem();
            finding.Message.ShouldContain("dependency");
            finding.Message.ShouldContain("ScopedDependency");
        }

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
