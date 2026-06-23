using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies.Handlers;
using Polly;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Decorators
{
    public class AsyncResiliencePipelineDecoratorCancellationTests
    {
        private const string PipelineName = "MyPipeline";

        private static UseResiliencePipelineHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result> BuildDecorator()
        {
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(PipelineName, (builder, _) =>
                builder.AddTimeout(TimeSpan.FromMinutes(1)));

            var decorator = new UseResiliencePipelineHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
            {
                Context = new QueryContext { ResiliencePipeline = registry }
            };
            decorator.InitializeFromAttributeParams(new object[] { PipelineName, false });
            return decorator;
        }

        [Fact]
        public async Task When_no_resilience_context_should_run_next_through_pipeline()
        {
            // Arrange
            var id = Guid.NewGuid();
            var decorator = BuildDecorator();
            var query = new AsyncTestQuery(id);
            var expected = new AsyncTestQuery.Result { Value = id };

            // Act — result flows back through GetPipeline(key).ExecuteAsync(...)
            var result = await decorator.ExecuteAsync(query,
                (q, ct) => Task.FromResult(expected),
                (q, ct) => Task.FromResult<AsyncTestQuery.Result>(null));

            // Assert
            result.ShouldBeSameAs(expected);
        }

        [Fact]
        public async Task When_token_already_cancelled_should_throw_OperationCanceledException()
        {
            // Arrange
            var decorator = BuildDecorator();
            var query = new AsyncTestQuery(Guid.NewGuid());
            var cancelled = new CancellationToken(canceled: true);

            // Act / Assert — the caller's cancelled token surfaces through the pipeline
            await Should.ThrowAsync<OperationCanceledException>(async () =>
                await decorator.ExecuteAsync(query,
                    (q, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        return Task.FromResult(new AsyncTestQuery.Result());
                    },
                    (q, ct) => Task.FromResult<AsyncTestQuery.Result>(null),
                    cancelled));
        }

        [Fact]
        public async Task When_token_cancelled_during_execution_should_abort()
        {
            // Arrange
            var decorator = BuildDecorator();
            var query = new AsyncTestQuery(Guid.NewGuid());
            using var cts = new CancellationTokenSource();

            // Act — handler is in flight (waiting on the token) when cancellation is requested
            var inFlight = decorator.ExecuteAsync(query,
                async (q, ct) =>
                {
                    await Task.Delay(Timeout.Infinite, ct);
                    return new AsyncTestQuery.Result();
                },
                (q, ct) => Task.FromResult<AsyncTestQuery.Result>(null),
                cts.Token);

            cts.Cancel();

            // Assert — cancellation aborts the in-flight execution (AC7)
            await Should.ThrowAsync<OperationCanceledException>(async () => await inFlight);
        }
    }
}
