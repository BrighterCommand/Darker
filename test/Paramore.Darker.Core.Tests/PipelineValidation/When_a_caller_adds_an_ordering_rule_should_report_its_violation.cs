using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class CallerSuppliedOrderingRuleTests
    {
        private static readonly ValidationRuleId CustomOrdering = new("CustomOrdering");
        private static readonly ValidationRuleId DecoratorOrdering = new("DecoratorOrdering");
        private const string CustomReason = "the custom inner must be wrapped by the custom outer";

        [Fact]
        public void When_a_caller_adds_an_ordering_rule_should_report_its_violation()
        {
            // Arrange — the default table extended with a custom pair; the handler violates both
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, CustomAndFallbackOrderingViolationHandler>();
            var customRule = new DecoratorOrderingRule(typeof(OuterDouble<,>), typeof(InnerDouble<,>), CustomOrdering, CustomReason);
            var pipelineValidator = new PipelineValidatorBuilder()
                .WithSyncRegistry(registry)
                .WithOrderingRules(DecoratorOrderingRules.Default.Append(customRule))
                .Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one warning per violated rule, each naming its decorators and reason
            var custom = result.Warnings.Where(w => w.RuleId == CustomOrdering).ToArray();
            custom.Length.ShouldBe(1);
            custom[0].Severity.ShouldBe(ValidationSeverity.Warning);
            custom[0].Message.ShouldContain("OuterDouble");
            custom[0].Message.ShouldContain("InnerDouble");
            custom[0].Message.ShouldContain(CustomReason);

            var core = result.Warnings.Where(w => w.RuleId == DecoratorOrdering).ToArray();
            core.Length.ShouldBe(1);
            core[0].Severity.ShouldBe(ValidationSeverity.Warning);
            core[0].Message.ShouldContain("FallbackPolicyDecorator");
            core[0].Message.ShouldContain("RetryableQueryDecorator");
            core[0].Message.ShouldContain("never retries");
        }
    }
}
