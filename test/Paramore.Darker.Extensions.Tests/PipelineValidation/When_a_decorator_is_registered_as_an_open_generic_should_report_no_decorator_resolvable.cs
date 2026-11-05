using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class OpenGenericDecoratorResolvabilityTests
    {
        [Fact]
        public void When_a_decorator_is_registered_as_an_open_generic_should_report_no_decorator_resolvable()
        {
            // Arrange — a handler decorated with the retry attribute, with the open generic decorator registered by AddDefaultPolicies
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddHandlers(r => r.Register<OpenGenericDecoratedQuery, OpenGenericDecoratedResult, OpenGenericDecoratedQueryHandler>())
                .AddDefaultPolicies()
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — the closed decorator counts as registered, so no DecoratorResolvable finding at all
            result.Errors.ShouldNotContain(e => e.RuleId == new ValidationRuleId("DecoratorResolvable"));
            result.Warnings.ShouldNotContain(w => w.RuleId == new ValidationRuleId("DecoratorResolvable"));
        }
    }
}
