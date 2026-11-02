using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class ConstructorReflectionFailureTests
    {
        [Fact]
        public void When_constructor_reflection_throws_should_report_rule_evaluation_failed()
        {
            // Arrange — reading the parameter's [FromKeyedServices] attribute throws inside the CaptiveDependency evaluator
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, ThrowingConstructorReflectionHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            PipelineValidationResult? result = null;
            Should.NotThrow(() => result = provider.ValidateAll());

            // Assert
            var error = result!.Errors.Where(e => e.RuleId == new ValidationRuleId("RuleEvaluationFailed")).ShouldHaveSingleItem();
            error.Source.ShouldContain("ThrowingConstructorReflectionHandler");
            error.Message.ShouldContain("The attribute constructor failed.");
            result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ShouldBeEmpty();
        }
    }
}
