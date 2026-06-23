using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_query_context_has_no_resilience_provider_should_set_provider_from_processor
    {
        [Fact]
        public void Execute_without_resilience_provider_on_context_should_use_processor_provider()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new ContextCapturingHandler();

            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, ContextCapturingHandler>();

            var handlerFactory = new SimpleHandlerFactory(type => handler);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            // A resilience pipeline provider owned by the processor
            var processorProvider = new ResiliencePipelineRegistry<string>();

            var queryProcessor = new QueryProcessor(
                handlerConfiguration,
                new InMemoryQueryContextFactory(),
                resiliencePipelineProvider: processorProvider);

            // Act — call Execute without providing a context
            queryProcessor.Execute(new SyncTestQuery(id), queryContext: null);

            // Assert — handler's Context.ResiliencePipeline is the same provider passed to the constructor
            handler.CapturedContext.ResiliencePipeline.ShouldBeSameAs(processorProvider);
            // And the processor does not populate ResilienceContext — it is caller-owned (FR9)
            handler.CapturedContext.ResilienceContext.ShouldBeNull();
        }
    }
}
