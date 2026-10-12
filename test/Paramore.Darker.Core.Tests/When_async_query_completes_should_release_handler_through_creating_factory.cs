using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_async_query_completes_should_release_handler_through_creating_factory
    {
        [Fact]
        public async Task When_execute_async_completes_should_release_the_handler_through_the_async_factory_only()
        {
            // Arrange — distinct sync and async factories, so a release reveals which one was used
            var handler = new AsyncLifetimeFaultQueryHandler();
            var syncFactory = new RecordingHandlerFactory(_ => handler);
            var asyncFactory = new RecordingHandlerFactory(_ => handler);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);

            var registryAsync = new QueryHandlerRegistryAsync();
            registryAsync.Register<AsyncLifetimeFaultQuery, AsyncLifetimeFaultResult, AsyncLifetimeFaultQueryHandler>();

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), syncFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                registryAsync, asyncFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new StreamQueryHandlerRegistry());
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            // Act
            await processor.ExecuteAsync(new AsyncLifetimeFaultQuery());

            // Assert — released once, by the factory that created it
            asyncFactory.ReleaseCount(handler).ShouldBe(1);
            syncFactory.ReleaseCount(handler).ShouldBe(0);
        }

        [Fact]
        public async Task When_execute_stream_completes_with_an_async_factory_should_release_the_handler_through_the_async_factory_only()
        {
            // Arrange — distinct sync and async factories, so a release reveals which one was used
            var handler = new StreamLifetimeFaultQueryHandler();
            var syncFactory = new RecordingHandlerFactory(_ => handler);
            var asyncFactory = new RecordingHandlerFactory(_ => handler);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);

            var registryStream = new StreamQueryHandlerRegistry();
            registryStream.Register<StreamLifetimeFaultQuery, StreamLifetimeFaultItem, StreamLifetimeFaultQueryHandler>();

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), syncFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new QueryHandlerRegistryAsync(), asyncFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                registryStream);
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            // Act
            await Drain(processor.ExecuteStream(new StreamLifetimeFaultQuery()));

            // Assert — released once, by the factory that created it
            asyncFactory.ReleaseCount(handler).ShouldBe(1);
            syncFactory.ReleaseCount(handler).ShouldBe(0);
        }

        [Fact]
        public async Task When_execute_stream_completes_with_no_async_factory_should_release_the_handler_through_the_sync_factory()
        {
            // Arrange — only a sync factory is configured
            var handler = new StreamLifetimeFaultQueryHandler();
            var syncFactory = new RecordingHandlerFactory(_ => handler);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);

            var registryStream = new StreamQueryHandlerRegistry();
            registryStream.Register<StreamLifetimeFaultQuery, StreamLifetimeFaultItem, StreamLifetimeFaultQueryHandler>();

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), syncFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                null, null, null, null,
                registryStream);
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            // Act
            await Drain(processor.ExecuteStream(new StreamLifetimeFaultQuery()));

            // Assert — released once, by the only factory there is
            syncFactory.ReleaseCount(handler).ShouldBe(1);
        }

        private static async Task Drain(IAsyncEnumerable<StreamLifetimeFaultItem> stream)
        {
            await foreach (var _ in stream)
            {
            }
        }
    }
}
