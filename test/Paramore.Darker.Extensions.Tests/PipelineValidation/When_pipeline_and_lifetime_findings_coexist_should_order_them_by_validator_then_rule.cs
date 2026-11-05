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
    public class FindingOrderAcrossValidatorsTests
    {
        [Fact]
        public void When_pipeline_and_lifetime_findings_coexist_should_order_them_by_validator_then_rule()
        {
            // Arrange — a pipeline Error and Warning, an inert JoinAmbient, two providers and two captive handlers
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<IAmAScopeProvider, TestScopeProvider>();
            services.AddSingleton<IAmAScopeProvider, AsyncLocalScopeProvider>();
            services.AddScoped<ScopedDependency>();
            var darker = services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Singleton;
                    options.DefaultScopeAffinity = ScopeAffinity.JoinAmbient;
                })
                .AddHandlers(r =>
                {
                    r.Register<UnregisteredDecoratorQuery, UnregisteredDecoratorResult, UnregisteredDecoratorQueryHandler>();
                    r.Register<DuplicateStepQuery, DuplicateStepResult, DuplicateStepQueryHandler>();
                    r.Register<CaptiveQuery, CaptiveResult, CaptiveHandler>();
                    r.Register<SecondCaptiveQuery, SecondCaptiveResult, SecondCaptiveHandler>();
                });
            darker.RegisterDecorator(typeof(InstanceRecordingDecorator<,>));
            darker.ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — pipeline findings precede lifetime findings, and DuplicateScopeProvider precedes CaptiveDependency
            var errorRules = new[] { "DecoratorResolvable", "InertScopeAffinity" };
            result.Errors
                .Select(e => e.RuleId.ToString())
                .Where(id => errorRules.Contains(id))
                .ToArray()
                .ShouldBe(new[] { "DecoratorResolvable", "InertScopeAffinity" });

            var warningRules = new[] { "DuplicateStep", "DuplicateScopeProvider", "CaptiveDependency" };
            var warnings = result.Warnings.Where(w => warningRules.Contains(w.RuleId.ToString())).ToArray();
            warnings.Select(w => w.RuleId.ToString()).ToArray()
                .ShouldBe(new[] { "DuplicateStep", "DuplicateScopeProvider", "CaptiveDependency", "CaptiveDependency" });

            var captive = warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ToArray();
            captive[0].Message.ShouldNotBe(captive[1].Message);
        }
    }
}
