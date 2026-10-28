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
    public class OrderingTableExtensibilityTests
    {
        [Fact]
        public void When_a_package_adds_an_ordering_rule_should_accumulate_it_beside_the_default_table()
        {
            // Arrange — a custom rule contributed with AddSingleton, and a handler that violates it and Retry/Fallback
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(new DecoratorOrderingRule(
                typeof(OuterDouble<,>), typeof(InnerDouble<,>), new ValidationRuleId("CustomOrdering"), "the outer double must wrap the inner double"));
            services.AddDarker()
                .AddHandlers(r => r.Register<OrderingViolationQuery, OrderingViolationResult, OrderingViolationQueryHandler>())
                .AddDefaultPolicies()
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — one warning from the custom rule and one from the default table
            var custom = result.Warnings.Single(w => w.RuleId == new ValidationRuleId("CustomOrdering"));
            custom.Severity.ShouldBe(ValidationSeverity.Warning);
            custom.Message.ShouldContain("OuterDouble");
            custom.Message.ShouldContain("InnerDouble");
            custom.Message.ShouldContain("the outer double must wrap the inner double");

            var builtIn = result.Warnings.Single(w => w.RuleId == new ValidationRuleId("DecoratorOrdering"));
            builtIn.Severity.ShouldBe(ValidationSeverity.Warning);
            builtIn.Message.ShouldContain("Retry");
            builtIn.Message.ShouldContain("Fallback");
        }
    }
}
