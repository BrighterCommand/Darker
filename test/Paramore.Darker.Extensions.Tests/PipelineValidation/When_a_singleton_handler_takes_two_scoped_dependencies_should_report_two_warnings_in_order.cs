using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class TwoCaptiveDependenciesTests
    {
        [Fact]
        public void When_a_singleton_handler_takes_two_scoped_dependencies_should_report_two_warnings_in_order()
        {
            // Arrange — a Singleton HandlerLifetime and a handler whose constructor takes two Scoped dependencies
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            services.AddScoped<SecondScopedDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<TwoScopedQuery, TwoScopedResult, TwoScopedHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container, without resolving the handler
            var result = provider.ValidateAll();

            // Assert — two Warnings, the first naming the first parameter and the second naming the second
            var captive = result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ToList();
            captive.Count.ShouldBe(2);
            captive[0].Message.ShouldContain("'first'");
            captive[0].Message.ShouldContain("ScopedDependency");
            captive[1].Message.ShouldContain("'second'");
            captive[1].Message.ShouldContain("SecondScopedDependency");
        }

        [Fact]
        public void When_two_singleton_handlers_each_take_a_scoped_dependency_should_report_a_warning_for_each_handler()
        {
            // Arrange — a Singleton HandlerLifetime and two handlers, each taking one Scoped dependency
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r =>
                {
                    r.Register<CaptiveQuery, CaptiveResult, CaptiveHandler>();
                    r.Register<SecondCaptiveQuery, SecondCaptiveResult, SecondCaptiveHandler>();
                })
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container, without resolving either handler
            var result = provider.ValidateAll();

            // Assert — one Warning per handler, each naming its own handler
            var captive = result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ToList();
            captive.Count.ShouldBe(2);
            captive.ShouldContain(w => w.Source == "Handler 'CaptiveHandler'");
            captive.ShouldContain(w => w.Source == "Handler 'SecondCaptiveHandler'");
        }
    }
}
