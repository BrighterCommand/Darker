using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class EqualWidthConstructorTests
    {
        [Fact]
        public void When_a_handler_has_two_equal_width_constructors_should_report_nothing()
        {
            // Arrange/Act — validate each handler (declared in opposite orders) under a Singleton HandlerLifetime
            var scopedFirst = CaptiveFindingsFor(r => r.Register<CaptiveQuery, CaptiveResult, EqualWidthScopedFirstConstructorHandler>());
            var otherFirst = CaptiveFindingsFor(r => r.Register<CaptiveQuery, CaptiveResult, EqualWidthOtherFirstConstructorHandler>());

            // Assert — neither yields a CaptiveDependency finding
            scopedFirst.ShouldBeEmpty();
            otherFirst.ShouldBeEmpty();
        }

        private static ValidationError[] CaptiveFindingsFor(System.Action<IQueryHandlerRegistry> register)
        {
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            services.AddSingleton<OtherDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(register)
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            return provider.ValidateAll().Warnings
                .Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency"))
                .ToArray();
        }
    }
}
