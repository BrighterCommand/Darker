using System;
using Paramore.Darker.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class When_executing_query_without_context_should_create_context_via_factory
    {
        [Fact]
        public void Execute_without_context_should_create_context_via_factory()
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

            var trackingFactory = new TrackingQueryContextFactory();
            var queryProcessor = new QueryProcessor(handlerConfiguration, trackingFactory);

            // Act — call Execute with explicit null context (no context provided by caller)
            var result = queryProcessor.Execute(new SyncTestQuery(id), queryContext: null);

            // Assert — factory was called, handler received non-null context, query succeeded
            trackingFactory.CreateCallCount.ShouldBe(1);
            result.ShouldNotBeNull();
            result.Value.ShouldBe(id);
            handler.CapturedContext.ShouldNotBeNull();
        }
    }
}
