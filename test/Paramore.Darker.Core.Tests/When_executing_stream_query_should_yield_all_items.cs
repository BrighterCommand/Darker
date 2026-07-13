using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_executing_stream_query_should_yield_all_items
    {
        private static QueryProcessor BuildProcessorWithAsyncFactories(StreamQueryHandlerRegistry streamRegistry)
        {
            // Wire up with the reused async factories (as per ADR §5 — stream handlers reuse async factory).
            var syncRegistry = new QueryHandlerRegistry();
            var handlerFactory = new SimpleHandlerFactory(_ => new MultiItemStreamHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }

        [Fact]
        public async Task When_executing_stream_query_should_yield_all_items_in_order()
        {
            // Arrange
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<MultiItemStreamQuery, string, MultiItemStreamHandler>();
            var processor = BuildProcessorWithAsyncFactories(streamRegistry);
            var query = new MultiItemStreamQuery();

            // Act
            var results = new List<string>();
            await foreach (var item in processor.ExecuteStream(query))
                results.Add(item);

            // Assert
            results.ShouldBe(MultiItemStreamHandler.Items);
        }

        [Fact]
        public async Task When_executing_stream_query_with_cancellation_token_should_yield_items()
        {
            // Arrange
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<MultiItemStreamQuery, string, MultiItemStreamHandler>();
            var processor = BuildProcessorWithAsyncFactories(streamRegistry);
            using var cts = new CancellationTokenSource();

            // Act
            var results = new List<string>();
            await foreach (var item in processor.ExecuteStream(new MultiItemStreamQuery(), cancellationToken: cts.Token))
                results.Add(item);

            // Assert
            results.ShouldBe(MultiItemStreamHandler.Items);
        }

        [Fact]
        public async Task When_executing_stream_query_with_provided_context_should_use_that_context()
        {
            // Arrange
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<MultiItemStreamQuery, string, MultiItemStreamHandler>();
            var processor = BuildProcessorWithAsyncFactories(streamRegistry);
            var queryContext = new InMemoryQueryContextFactory().Create();
            queryContext.Bag["test-key"] = "test-value";

            // Act
            var results = new List<string>();
            await foreach (var item in processor.ExecuteStream(new MultiItemStreamQuery(), queryContext: queryContext))
                results.Add(item);

            // Assert — items yielded and the provided context was used (not replaced)
            results.ShouldBe(MultiItemStreamHandler.Items);
            queryContext.Bag["test-key"].ShouldBe("test-value");
        }
    }
}
