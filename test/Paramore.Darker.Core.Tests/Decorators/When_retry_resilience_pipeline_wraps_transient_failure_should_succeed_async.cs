using System;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies.Handlers;
using Polly;
using Polly.Registry;
using Polly.Retry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Decorators
{
    public class When_retry_resilience_pipeline_wraps_transient_failure_should_succeed_async
    {
        [Fact]
        public async Task ExecuteAsync_through_zero_delay_retry_pipeline_should_retry_to_success()
        {
            // Arrange — a zero-delay retry pipeline that handles the transient exception
            const string pipelineName = "Retry";
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(pipelineName, (builder, _) =>
                builder.AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.Zero
                }));

            var context = new QueryContext { ResiliencePipeline = registry };
            var decorator = new UseResiliencePipelineHandlerAsync<SyncTestQuery, SyncTestQuery.Result>
            {
                Context = context
            };
            decorator.InitializeFromAttributeParams(new object[] { pipelineName, false });

            // The handler fails twice, then succeeds on the third call
            var id = Guid.NewGuid();
            var handler = new TransientlyFailingHandler(failuresBeforeSuccess: 2);

            // Act
            var result = await decorator.ExecuteAsync(new SyncTestQuery(id),
                (q, ct) => handler.ExecuteAsync(q, ct),
                (q, ct) => Task.FromResult<SyncTestQuery.Result>(null));

            // Assert — the retry pipeline drove the handler to success
            result.Value.ShouldBe(id);
            handler.Calls.ShouldBe(3);
        }
    }
}
