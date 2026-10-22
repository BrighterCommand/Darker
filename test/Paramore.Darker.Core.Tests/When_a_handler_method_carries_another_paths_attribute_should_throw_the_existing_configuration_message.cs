using System;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class WrongKindAttributeMessageTests
    {
        private static QueryProcessor BuildProcessor(
            QueryHandlerRegistry syncRegistry = null,
            QueryHandlerRegistryAsync asyncRegistry = null,
            StreamQueryHandlerRegistry streamRegistry = null)
        {
            var handlerFactory = new SimpleHandlerFactory(type => (IQueryHandler)Activator.CreateInstance(type));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var configuration = new HandlerConfiguration(
                syncRegistry ?? new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry ?? new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry ?? new StreamQueryHandlerRegistry());

            return new QueryProcessor(configuration, new InMemoryQueryContextFactory());
        }

        // BuildStream runs inside the iterator, so the exception surfaces on the first MoveNextAsync.
        private static async Task<Exception> FirstMoveNextFailure(QueryProcessor queryProcessor)
        {
            var enumerator = queryProcessor.ExecuteStream(new StreamTestQuery()).GetAsyncEnumerator();
            try
            {
                await enumerator.MoveNextAsync();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        [Fact]
        public void When_a_handler_method_carries_another_paths_attribute_should_throw_the_existing_configuration_message()
        {
            // Arrange — a sync handler whose Execute carries an async attribute
            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithAsyncAttribute>();
            var queryProcessor = BuildProcessor(syncRegistry: syncRegistry);

            // Act
            var exception = Should.Throw<ConfigurationException>(
                () => queryProcessor.Execute(new SyncTestQuery(Guid.NewGuid())));

            // Assert
            exception.Message.ShouldBe(
                "Sync handler has async attribute(s) on Execute. Use sync attributes (e.g. QueryHandlerAttribute) for sync handlers, or switch to an async handler with ExecuteAsync.");
        }

        [Fact]
        public void When_a_sync_handler_carries_a_stream_attribute_should_throw_the_existing_configuration_message()
        {
            // Arrange — a sync handler whose Execute carries a stream attribute
            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithStreamAttribute>();
            var queryProcessor = BuildProcessor(syncRegistry: syncRegistry);

            // Act
            var exception = Should.Throw<ConfigurationException>(
                () => queryProcessor.Execute(new SyncTestQuery(Guid.NewGuid())));

            // Assert
            exception.Message.ShouldBe(
                "Sync handler has stream attribute(s) on Execute. Use StreamQueryHandlerAttribute only on stream handlers implementing IStreamQueryHandler.");
        }

        [Fact]
        public async Task When_an_async_handler_carries_a_sync_attribute_should_throw_the_existing_configuration_message()
        {
            // Arrange — an async handler whose ExecuteAsync carries a sync attribute
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncHandlerWithSyncAttribute>();
            var queryProcessor = BuildProcessor(asyncRegistry: asyncRegistry);

            // Act
            var exception = await Should.ThrowAsync<ConfigurationException>(
                () => queryProcessor.ExecuteAsync(new AsyncTestQuery(Guid.NewGuid()),
                    cancellationToken: TestContext.Current.CancellationToken));

            // Assert
            exception.Message.ShouldBe(
                "Async handler has sync attribute(s) on ExecuteAsync. Use async attributes (e.g. QueryHandlerAttributeAsync) for async handlers, or switch to a sync handler with Execute.");
        }

        [Fact]
        public async Task When_an_async_handler_carries_a_stream_attribute_should_throw_the_existing_configuration_message()
        {
            // Arrange — an async handler whose ExecuteAsync carries a stream attribute
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncHandlerWithStreamAttribute>();
            var queryProcessor = BuildProcessor(asyncRegistry: asyncRegistry);

            // Act
            var exception = await Should.ThrowAsync<ConfigurationException>(
                () => queryProcessor.ExecuteAsync(new AsyncTestQuery(Guid.NewGuid()),
                    cancellationToken: TestContext.Current.CancellationToken));

            // Assert
            exception.Message.ShouldBe(
                "Async handler has stream attribute(s) on ExecuteAsync. Use StreamQueryHandlerAttribute only on stream handlers implementing IStreamQueryHandler.");
        }

        [Fact]
        public async Task When_a_stream_handler_carries_a_sync_attribute_should_throw_the_existing_configuration_message()
        {
            // Arrange — a stream handler whose ExecuteAsync carries a sync attribute
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<StreamTestQuery, string, StreamHandlerWithSyncAttribute>();
            var queryProcessor = BuildProcessor(streamRegistry: streamRegistry);

            // Act
            var exception = await FirstMoveNextFailure(queryProcessor);

            // Assert
            exception.ShouldBeOfType<ConfigurationException>();
            exception.Message.ShouldBe(
                "Stream handler has sync attribute(s) on ExecuteAsync. Use StreamQueryHandlerAttribute for stream handlers.");
        }

        [Fact]
        public async Task When_a_stream_handler_carries_an_async_attribute_should_throw_the_existing_configuration_message()
        {
            // Arrange — a stream handler whose ExecuteAsync carries an async attribute
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<StreamTestQuery, string, StreamHandlerWithAsyncAttribute>();
            var queryProcessor = BuildProcessor(streamRegistry: streamRegistry);

            // Act
            var exception = await FirstMoveNextFailure(queryProcessor);

            // Assert
            exception.ShouldBeOfType<ConfigurationException>();
            exception.Message.ShouldBe(
                "Stream handler has async attribute(s) on ExecuteAsync. Use StreamQueryHandlerAttribute for stream handlers.");
        }
    }
}
