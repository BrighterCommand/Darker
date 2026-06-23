using System;
using System.Collections.Generic;
using Paramore.Darker.Core.Tests.TestDoubles;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_caller_provides_context_with_resilience_provider_should_preserve_caller_provider
    {
        [Fact]
        public void Execute_with_caller_context_provider_should_not_be_overwritten_by_processor_provider()
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

            // Processor has its own resilience pipeline provider
            var processorProvider = new ResiliencePipelineRegistry<string>();

            var queryProcessor = new QueryProcessor(
                handlerConfiguration,
                new InMemoryQueryContextFactory(),
                resiliencePipelineProvider: processorProvider);

            // Caller provides a context with a different resilience pipeline provider
            var callerProvider = new ResiliencePipelineRegistry<string>();
            var callerContext = new QueryContext
            {
                Bag = new Dictionary<string, object>(),
                ResiliencePipeline = callerProvider
            };

            // Act — call Execute with caller-provided context that already has ResiliencePipeline set
            queryProcessor.Execute(new SyncTestQuery(id), queryContext: callerContext);

            // Assert — callerProvider wins, processorProvider is not used (fill-if-absent)
            handler.CapturedContext.ResiliencePipeline.ShouldBeSameAs(callerProvider);
            handler.CapturedContext.ResiliencePipeline.ShouldNotBeSameAs(processorProvider);
        }
    }
}
