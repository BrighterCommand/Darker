using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class CorrectDecoratorOrderingRuleTests
    {
        private static readonly ValidationRuleId DecoratorOrdering = new("DecoratorOrdering");

        [Fact]
        public void When_a_fallback_sits_outside_a_retry_should_report_no_decorator_ordering()
        {
            // Arrange — Fallback at Step 1 (outer) wraps Retry at Step 2 (inner), on the sync and async paths
            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, FallbackOutsideRetryHandler>();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncFallbackOutsideRetryHandler>();
            var pipelineValidator = new PipelineValidatorBuilder()
                .WithSyncRegistry(syncRegistry)
                .WithAsyncRegistry(asyncRegistry)
                .Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — no ordering finding of any severity
            result.Warnings.Where(w => w.RuleId == DecoratorOrdering).ShouldBeEmpty();
            result.Errors.Where(e => e.RuleId == DecoratorOrdering).ShouldBeEmpty();
        }
    }
}
