using System;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Policies.Handlers;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Decorators
{
    public class When_use_type_pipeline_set_should_isolate_breaker_state_per_result_type
    {
        private const string Key = "CircuitBreaker";

        private static void AddTypedBreaker<TResult>(ResiliencePipelineRegistry<string> registry)
        {
            registry.TryAddBuilder<TResult>(Key, (builder, _) =>
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<TResult>
                {
                    ShouldHandle = new PredicateBuilder<TResult>().Handle<InvalidOperationException>(),
                    FailureRatio = 1.0,
                    MinimumThroughput = 2,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(30)
                }));
        }

        private static UseResiliencePipelineHandler<TQuery, TResult> TypeScopedDecorator<TQuery, TResult>(
            ResiliencePipelineRegistry<string> registry)
            where TQuery : IQuery<TResult>
        {
            var decorator = new UseResiliencePipelineHandler<TQuery, TResult>
            {
                Context = new QueryContext { ResiliencePipeline = registry }
            };
            decorator.InitializeFromAttributeParams(new object[] { Key, true });
            return decorator;
        }

        [Fact]
        public void Different_result_types_under_the_same_key_open_independently()
        {
            // Arrange — a typed breaker registered for each of two distinct result types
            var registry = new ResiliencePipelineRegistry<string>();
            AddTypedBreaker<SyncTestQuery.Result>(registry);
            AddTypedBreaker<AsyncTestQuery.Result>(registry);

            var decoratorA = TypeScopedDecorator<SyncTestQuery, SyncTestQuery.Result>(registry);
            var decoratorB = TypeScopedDecorator<AsyncTestQuery, AsyncTestQuery.Result>(registry);

            var queryA = new SyncTestQuery(Guid.NewGuid());
            var queryB = new AsyncTestQuery(Guid.NewGuid());

            // Act — open result-type-A's breaker with two consecutive failures
            Should.Throw<InvalidOperationException>(() =>
                decoratorA.Execute(queryA, q => throw new InvalidOperationException(), q => null));
            Should.Throw<InvalidOperationException>(() =>
                decoratorA.Execute(queryA, q => throw new InvalidOperationException(), q => null));
            Should.Throw<BrokenCircuitException>(() =>
                decoratorA.Execute(queryA, q => throw new InvalidOperationException(), q => null));

            // Assert — result-type-B's breaker is independent: still closed, so the handler is
            // invoked and its own exception surfaces (not BrokenCircuitException)
            Should.Throw<InvalidOperationException>(() =>
                decoratorB.Execute(queryB, q => throw new InvalidOperationException(), q => null));
        }

        [Fact]
        public void Same_result_type_under_the_same_key_shares_breaker_state()
        {
            // Arrange — one typed breaker; two decorator instances over the SAME result type
            var registry = new ResiliencePipelineRegistry<string>();
            AddTypedBreaker<SyncTestQuery.Result>(registry);

            var decoratorA = TypeScopedDecorator<SyncTestQuery, SyncTestQuery.Result>(registry);
            var decoratorB = TypeScopedDecorator<SyncTestQuery, SyncTestQuery.Result>(registry);
            var query = new SyncTestQuery(Guid.NewGuid());

            // Act — open the breaker through decorator A
            Should.Throw<InvalidOperationException>(() =>
                decoratorA.Execute(query, q => throw new InvalidOperationException(), q => null));
            Should.Throw<InvalidOperationException>(() =>
                decoratorA.Execute(query, q => throw new InvalidOperationException(), q => null));

            // Assert — decorator B resolves the same (key, TResult) instance, so it sees the open circuit
            Should.Throw<BrokenCircuitException>(() =>
                decoratorB.Execute(query, q => throw new InvalidOperationException(), q => null));
        }

        [Fact]
        public void UseTypePipeline_with_only_a_non_generic_builder_fails_validation()
        {
            // Arrange — the key has only a NON-generic builder registered
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(Key, (builder, _) => builder.AddTimeout(TimeSpan.FromMinutes(1)));

            var decorator = new UseResiliencePipelineHandler<SyncTestQuery, SyncTestQuery.Result>
            {
                Context = new QueryContext { ResiliencePipeline = registry }
            };

            // Act — generic and non-generic registrations are separate namespaces
            var exception = Should.Throw<ConfigurationException>(() =>
                decorator.InitializeFromAttributeParams(new object[] { Key, true }));

            // Assert — the unresolved key is named
            exception.Message.ShouldContain(Key);
        }

        [Fact]
        public async Task Async_type_scoped_decorator_executes_through_the_typed_pipeline()
        {
            // Arrange — a typed pipeline for the result type
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder<SyncTestQuery.Result>(Key, (builder, _) =>
                builder.AddTimeout(TimeSpan.FromMinutes(1)));

            var decorator = new UseResiliencePipelineHandlerAsync<SyncTestQuery, SyncTestQuery.Result>
            {
                Context = new QueryContext { ResiliencePipeline = registry }
            };
            decorator.InitializeFromAttributeParams(new object[] { Key, true });

            var id = Guid.NewGuid();
            var expected = new SyncTestQuery.Result { Value = id };

            // Act
            var result = await decorator.ExecuteAsync(new SyncTestQuery(id),
                (q, ct) => Task.FromResult(expected),
                (q, ct) => Task.FromResult<SyncTestQuery.Result>(null));

            // Assert — result flows back through the typed pipeline
            result.ShouldBeSameAs(expected);
        }
    }
}
