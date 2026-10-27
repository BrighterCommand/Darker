using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Policies.Handlers;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class DecoratorOrderingRuleTests
    {
        private static readonly ValidationRuleId DecoratorOrdering = new("DecoratorOrdering");

        [Fact]
        public void When_a_fallback_sits_inside_a_retry_should_report_decorator_ordering()
        {
            // Arrange — Retry at Step 1 (outer) wraps Fallback at Step 2 (inner)
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, RetryOutsideFallbackHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one warning naming the handler, both decorators and the reason
            var findings = result.Warnings.Where(w => w.RuleId == DecoratorOrdering).ToArray();
            findings.Length.ShouldBe(1);
            findings[0].Severity.ShouldBe(ValidationSeverity.Warning);
            findings[0].Message.ShouldContain(nameof(RetryOutsideFallbackHandler));
            findings[0].Message.ShouldContain("FallbackPolicyDecorator");
            findings[0].Message.ShouldContain("RetryableQueryDecorator");
            findings[0].Message.ShouldContain("never retries");
        }

        [Fact]
        public void When_a_custom_ordering_rule_is_violated_should_report_the_entry_rule_id()
        {
            // Arrange — a table holding only a custom entry
            var customId = new ValidationRuleId("CustomOrdering");
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, RetryOutsideFallbackHandler>();
            var pipelineValidator = new PipelineValidatorBuilder()
                .WithSyncRegistry(registry)
                .WithOrderingRules([new DecoratorOrderingRule(typeof(FallbackPolicyDecorator<,>), typeof(RetryableQueryDecorator<,>), customId, "reason")])
                .Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — the finding carries the entry's identifier, not a hard-coded one
            result.Warnings.Where(w => w.RuleId == customId).ToArray().Length.ShouldBe(1);
            result.Warnings.Where(w => w.RuleId == DecoratorOrdering).ShouldBeEmpty();
        }
    }
}
