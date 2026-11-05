using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class CombinedPipelineAndLifetimeFindingsTests
    {
        [Fact]
        public void When_lifetime_and_pipeline_mistakes_coexist_should_report_all_in_one_result()
        {
            // Arrange — an unregistered decorator, an inert JoinAmbient (Singleton handlers), two different providers and a captive handler
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<IAmAScopeProvider, TestScopeProvider>();
            services.AddSingleton<IAmAScopeProvider, AsyncLocalScopeProvider>();
            services.AddScoped<ScopedDependency>();
            services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Singleton;
                    options.DefaultScopeAffinity = ScopeAffinity.JoinAmbient;
                })
                .AddHandlers(r =>
                {
                    r.Register<UnregisteredDecoratorQuery, UnregisteredDecoratorResult, UnregisteredDecoratorQueryHandler>();
                    r.Register<CaptiveQuery, CaptiveResult, CaptiveHandler>();
                })
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — all four rules appear in the one combined result
            result.Errors.ShouldContain(e => e.RuleId == new ValidationRuleId("DecoratorResolvable"));
            result.Errors.ShouldContain(e => e.RuleId == new ValidationRuleId("InertScopeAffinity"));
            result.Warnings.ShouldContain(w => w.RuleId == new ValidationRuleId("DuplicateScopeProvider"));
            result.Warnings.ShouldContain(w => w.RuleId == new ValidationRuleId("CaptiveDependency"));
        }
    }
}
