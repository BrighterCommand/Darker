using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class FailedDecoratorDuplicateStepRuleTests
    {
        private static readonly ValidationRuleId DuplicateStep = new("DuplicateStep");
        private static readonly ValidationRuleId PipelineDescribable = new("PipelineDescribable");

        [Fact]
        public void When_a_failed_decorator_shares_a_step_should_report_no_duplicate_step()
        {
            // Arrange — a valid attribute and an unclosable one, both at Step 1
            var registry = new QueryHandlerRegistryAsync();
            registry.Register<AsyncTestQuery, AsyncTestQuery.Result, FailedDecoratorSharesStepAsyncHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — the failed decorator adds no step, so nothing duplicates it
            result.Warnings.ShouldNotContain(w => w.RuleId == DuplicateStep);
            result.Errors.Count(e => e.RuleId == PipelineDescribable).ShouldBe(1);
        }
    }
}
