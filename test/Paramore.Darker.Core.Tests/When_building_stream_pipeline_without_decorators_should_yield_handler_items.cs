using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_building_stream_pipeline_without_decorators_should_yield_handler_items
    {
        private static QueryProcessor BuildProcessor(StreamQueryHandlerRegistry streamRegistry)
        {
            var syncRegistry = new QueryHandlerRegistry();
            var handlerFactory = new SimpleHandlerFactory(_ => new StreamTestQueryHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                null, null, null, null,
                streamRegistry);

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }

        [Fact]
        public async Task When_executing_stream_query_should_yield_items_in_order()
        {
            // Arrange
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<StreamTestQuery, string, StreamTestQueryHandler>();
            var processor = BuildProcessor(streamRegistry);

            // Act
            var results = new List<string>();
            await foreach (var item in processor.ExecuteStream(new StreamTestQuery()))
                results.Add(item);

            // Assert
            results.ShouldBe(new[] { "item" });
        }

        [Fact]
        public async Task When_handler_has_ambiguous_ExecuteAsync_should_resolve_stream_signature_without_AmbiguousMatchException()
        {
            // Arrange — DualExecuteStreamHandler exposes two ExecuteAsync overloads;
            // BuildStream must resolve by return type + params, not bare method name.
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<StreamTestQuery, string, DualExecuteStreamHandler>();

            var syncRegistry = new QueryHandlerRegistry();
            var handlerFactory = new SimpleHandlerFactory(_ => new DualExecuteStreamHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                null, null, null, null,
                streamRegistry);
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            // Act
            var results = new List<string>();
            await foreach (var item in processor.ExecuteStream(new StreamTestQuery()))
                results.Add(item);

            // Assert — yielded the stream items, not the Task overload result
            results.ShouldBe(DualExecuteStreamHandler.Items);
        }
    }
}
