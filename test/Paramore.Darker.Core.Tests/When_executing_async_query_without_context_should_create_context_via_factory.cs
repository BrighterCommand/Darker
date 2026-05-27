using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_executing_async_query_without_context_should_create_context_via_factory
    {
        [Fact]
        public async Task ExecuteAsync_without_context_should_create_context_via_factory()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new AsyncContextCapturingHandler();

            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncContextCapturingHandler>();

            var handlerFactory = new SimpleHandlerFactory(type => handler);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var trackingFactory = new TrackingQueryContextFactory();
            var queryProcessor = new QueryProcessor(handlerConfiguration, trackingFactory);

            // Act — call ExecuteAsync with explicit null context (no context provided by caller)
            var result = await queryProcessor.ExecuteAsync(new AsyncTestQuery(id), queryContext: null, cancellationToken: CancellationToken.None);

            // Assert — factory was called, handler received non-null context, query succeeded
            trackingFactory.CreateCallCount.ShouldBe(1);
            result.ShouldNotBeNull();
            result.Value.ShouldBe(id);
            handler.CapturedContext.ShouldNotBeNull();
        }
    }
}
