using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class ServiceProviderDecoratorTests
    {
        [Fact]
        public void When_a_singleton_open_generic_decorator_takes_an_IServiceProvider_should_report_nothing()
        {
            // Arrange — a Singleton HandlerLifetime and an open-generic decorator taking IServiceProvider
            var services = new ServiceCollection();
            var builder = services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton);
            builder.RegisterDecorator(typeof(ServiceProviderDecorator<,>));
            builder.ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert — no CaptiveDependency, no RuleEvaluationFailed, no errors
            result.Errors.Concat(result.Warnings).ShouldNotContain(f => f.RuleId == new ValidationRuleId("CaptiveDependency"));
            result.Errors.Concat(result.Warnings).ShouldNotContain(f => f.RuleId == new ValidationRuleId("RuleEvaluationFailed"));
            result.Errors.ShouldBeEmpty();
        }
    }
}
