using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class AsyncDecoratorOrderingRuleTests
    {
        private static readonly ValidationRuleId DecoratorOrdering = new("DecoratorOrdering");

        [Fact]
        public void When_an_async_fallback_sits_inside_an_async_retry_should_report_decorator_ordering()
        {
            // Arrange — async Retry at Step 1 (outer) wraps async Fallback at Step 2 (inner)
            var registry = new QueryHandlerRegistryAsync();
            registry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncRetryOutsideFallbackHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one warning naming the handler and both async decorators
            var findings = result.Warnings.Where(w => w.RuleId == DecoratorOrdering).ToArray();
            findings.Length.ShouldBe(1);
            findings[0].Message.ShouldContain(nameof(AsyncRetryOutsideFallbackHandler));
            findings[0].Message.ShouldContain("FallbackPolicyDecoratorAsync");
            findings[0].Message.ShouldContain("RetryableQueryDecoratorAsync");
        }
    }
}
