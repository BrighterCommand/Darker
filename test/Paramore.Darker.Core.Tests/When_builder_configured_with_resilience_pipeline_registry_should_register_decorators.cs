using System;
using Paramore.Darker.Builder;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies;
using Polly;
using Polly.Registry;
using Polly.Retry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_builder_configured_with_resilience_pipeline_registry_should_register_decorators
    {
        private static ResiliencePipelineRegistry<string> RetryRegistry()
        {
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder("Retry", (builder, _) =>
                builder.AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.Zero
                }));
            return registry;
        }

        private static QueryHandlerRegistry SyncRegistryFor(ResiliencePipelineRetryHandler handler)
        {
            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, ResiliencePipelineRetryHandler>();
            return syncRegistry;
        }

        [Fact]
        public void Build_with_resilience_pipelines_should_execute_decorated_handler_through_named_pipeline()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new ResiliencePipelineRetryHandler();
            var registry = RetryRegistry();

            var queryProcessor = QueryProcessorBuilder
                .With()
                .Handlers(
                    SyncRegistryFor(handler),
                    type => handler,
                    type => { },
                    type => (IQueryHandlerDecorator)Activator.CreateInstance(type))
                .InMemoryQueryContextFactory()
                .ResiliencePipelines(registry)
                .Build();

            // Act — the decorated handler fails twice then succeeds, driven by the named retry pipeline
            var result = queryProcessor.Execute(new SyncTestQuery(id));

            // Assert — proves the decorators are wired and the registry was threaded onto the context
            result.Value.ShouldBe(id);
        }

        [Fact]
        public void ResiliencePipelines_with_null_registry_should_throw_ArgumentNullException()
        {
            // Arrange
            var handler = new ResiliencePipelineRetryHandler();
            var stage = QueryProcessorBuilder
                .With()
                .Handlers(
                    SyncRegistryFor(handler),
                    type => handler,
                    type => { },
                    type => null)
                .InMemoryQueryContextFactory();

            // Act / Assert — null guard only, no legacy content-key check
            Should.Throw<ArgumentNullException>(() => stage.ResiliencePipelines(null));
        }

        [Fact]
        public void ResiliencePipelines_should_set_the_registry_on_the_builder()
        {
            // Arrange
            var handler = new ResiliencePipelineRetryHandler();
            var registry = RetryRegistry();
            var stage = QueryProcessorBuilder
                .With()
                .Handlers(
                    SyncRegistryFor(handler),
                    type => handler,
                    type => { },
                    type => null)
                .InMemoryQueryContextFactory();

            // Act
            stage.ResiliencePipelines(registry);

            // Assert — the supplied registry is set on the builder so Build() threads it onto the processor
            ((QueryProcessorBuilder)stage).ResiliencePipelineRegistry.ShouldBeSameAs(registry);
        }
    }
}
