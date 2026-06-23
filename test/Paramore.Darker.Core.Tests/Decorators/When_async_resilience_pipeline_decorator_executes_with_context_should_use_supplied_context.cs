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
    public class AsyncResiliencePipelineDecoratorResilienceContextTests
    {
        private const string PipelineName = "MyPipeline";

        [Fact]
        public async Task When_resilience_context_supplied_should_pass_it_to_the_pipeline()
        {
            // Arrange — a strategy that reads a property off the executing resilience context
            var key = new ResiliencePropertyKey<string>("greeting");
            var strategy = new PropertyCapturingStrategy(key);

            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(PipelineName, (builder, _) => builder.AddStrategy(_ => strategy));

            var resilienceContext = ResilienceContextPool.Shared.Get();
            resilienceContext.Properties.Set(key, "hello");

            var decorator = new UseResiliencePipelineHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
            {
                Context = new QueryContext
                {
                    ResiliencePipeline = registry,
                    ResilienceContext = resilienceContext
                }
            };
            decorator.InitializeFromAttributeParams(new object[] { PipelineName, false });

            var id = Guid.NewGuid();
            var expected = new AsyncTestQuery.Result { Value = id };

            // Act
            var result = await decorator.ExecuteAsync(new AsyncTestQuery(id),
                (q, ct) => Task.FromResult(expected),
                (q, ct) => Task.FromResult<AsyncTestQuery.Result>(null));

            // Assert — result flows back and the strategy saw the property from the supplied context
            result.ShouldBeSameAs(expected);
            strategy.CapturedValue.ShouldBe("hello");

            ResilienceContextPool.Shared.Return(resilienceContext);
        }

        [Fact]
        public async Task When_resilience_context_supplied_should_use_its_token_and_not_merge_caller_token()
        {
            // Arrange — the supplied context carries a non-cancelled token
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(PipelineName, (builder, _) =>
                builder.AddTimeout(TimeSpan.FromMinutes(1)));

            var resilienceContext = ResilienceContextPool.Shared.Get(CancellationToken.None);

            var decorator = new UseResiliencePipelineHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
            {
                Context = new QueryContext
                {
                    ResiliencePipeline = registry,
                    ResilienceContext = resilienceContext
                }
            };
            decorator.InitializeFromAttributeParams(new object[] { PipelineName, false });

            var id = Guid.NewGuid();
            var expected = new AsyncTestQuery.Result { Value = id };
            var cancelledCaller = new CancellationToken(canceled: true);

            // Act — caller passes an already-cancelled token; the context's token is authoritative
            // (tokens are not merged), so the handler observes a non-cancelled token and succeeds.
            var result = await decorator.ExecuteAsync(new AsyncTestQuery(id),
                (q, ct) =>
                {
                    ct.IsCancellationRequested.ShouldBeFalse();
                    return Task.FromResult(expected);
                },
                (q, ct) => Task.FromResult<AsyncTestQuery.Result>(null),
                cancelledCaller);

            // Assert
            result.ShouldBeSameAs(expected);

            ResilienceContextPool.Shared.Return(resilienceContext);
        }
    }
}
