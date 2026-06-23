using Paramore.Darker.Builder;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_DefaultResiliencePipelines_called_should_register_resolvable_default_pipelines
    {
        [Fact]
        public void Default_pipelines_should_be_resolvable_and_executable_under_their_well_known_keys()
        {
            // Arrange
            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, ResiliencePipelineRetryHandler>();

            var stage = QueryProcessorBuilder
                .With()
                .Handlers(
                    syncRegistry,
                    type => new ResiliencePipelineRetryHandler(),
                    type => { },
                    type => null)
                .InMemoryQueryContextFactory();

            // Act
            stage.DefaultResiliencePipelines();

            // Assert — the registry threaded onto the builder resolves an executable pipeline under each key
            var registry = ((QueryProcessorBuilder)stage).ResiliencePipelineRegistry;
            registry.ShouldNotBeNull();

            var retry = registry.GetPipeline(Constants.RetryPipelineName);
            var breaker = registry.GetPipeline(Constants.CircuitBreakerPipelineName);

            retry.Execute(() => 42).ShouldBe(42);
            breaker.Execute(() => 7).ShouldBe(7);

            // And the default pipeline keys do not collide with the legacy policy keys (RD4)
            Constants.RetryPipelineName.ShouldNotBe(Constants.RetryPolicyName);
            Constants.CircuitBreakerPipelineName.ShouldNotBe(Constants.CircuitBreakerPolicyName);
        }
    }
}
