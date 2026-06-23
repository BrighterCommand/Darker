using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies.Handlers;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Decorators
{
    public class When_circuit_breaker_resilience_pipeline_reaches_threshold_should_open_sync
    {
        [Fact]
        public void Execute_after_threshold_consecutive_failures_should_open_the_circuit()
        {
            // Arrange — a breaker that opens after two consecutive failures (100% failure ratio)
            const string pipelineName = "CircuitBreaker";
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(pipelineName, (builder, _) =>
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
                    FailureRatio = 1.0,
                    MinimumThroughput = 2,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(30)
                }));

            var decorator = new UseResiliencePipelineHandler<SyncTestQuery, SyncTestQuery.Result>
            {
                Context = new QueryContext { ResiliencePipeline = registry }
            };
            decorator.InitializeFromAttributeParams(new object[] { pipelineName, false });

            // A handler that always fails
            var handler = new TransientlyFailingHandler(failuresBeforeSuccess: int.MaxValue);
            var query = new SyncTestQuery(Guid.NewGuid());

            // Act — two failing calls reach the threshold and open the circuit
            Should.Throw<InvalidOperationException>(() =>
                decorator.Execute(query, q => handler.Execute(q), q => null));
            Should.Throw<InvalidOperationException>(() =>
                decorator.Execute(query, q => handler.Execute(q), q => null));

            // Assert — the next call is rejected by the open circuit without invoking the handler
            Should.Throw<BrokenCircuitException>(() =>
                decorator.Execute(query, q => handler.Execute(q), q => null));
            handler.Calls.ShouldBe(2);
        }
    }
}
