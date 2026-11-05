using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class SameRuleFailingTwiceTests
    {
        private static readonly ValidationRuleId DuplicateStep = new("DuplicateStep");

        [Fact]
        public void When_two_handlers_fail_the_same_rule_should_report_a_finding_for_each()
        {
            // Arrange — two queries on the sync path, each with a handler that has a duplicate step
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, DuplicateStepSyncHandler>();
            registry.Register<DatedQuery, string, DuplicateStepDatedQueryHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one DuplicateStep warning per handler, each naming its own handler
            var findings = result.Warnings.Where(w => w.RuleId == DuplicateStep).ToList();
            findings.Count.ShouldBe(2);
            findings.ShouldContain(f => f.Message.Contains(nameof(DuplicateStepSyncHandler)));
            findings.ShouldContain(f => f.Message.Contains(nameof(DuplicateStepDatedQueryHandler)));
        }

        [Fact]
        public void When_two_handlers_fail_the_same_rule_should_report_a_finding_for_each_time_validate_is_called()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, DuplicateStepSyncHandler>();
            registry.Register<DatedQuery, string, DuplicateStepDatedQueryHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var first = pipelineValidator.Validate();
            var second = pipelineValidator.Validate();

            // Assert — the validator keeps no state between calls
            second.Warnings.Count.ShouldBe(first.Warnings.Count);
            second.Warnings.ShouldBe(first.Warnings);
            second.Errors.ShouldBe(first.Errors);
        }
    }
}
