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
    public class DecoratorResolvabilityTests
    {
        [Fact]
        public void When_a_decorator_is_not_registered_in_the_container_should_report_decorator_resolvable()
        {
            // Arrange — a handler whose attribute names a decorator that nothing registers
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddHandlers(r => r.Register<UnregisteredDecoratorQuery, UnregisteredDecoratorResult, UnregisteredDecoratorQueryHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — an Error names the decorator and the handler, and resolvability was checked
            var error = result.Errors.Single(e => e.RuleId == new ValidationRuleId("DecoratorResolvable"));
            error.Severity.ShouldBe(ValidationSeverity.Error);
            error.Message.ShouldContain("UnregisteredDecorator");
            error.Message.ShouldContain(nameof(UnregisteredDecoratorQueryHandler));
            result.Warnings.ShouldNotContain(w => w.RuleId == new ValidationRuleId("ResolvabilityNotChecked"));
        }
    }
}
