using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class StreamContextRoutingTests
    {
        [Fact]
        public async Task When_stream_routing_function_reads_context_should_route_by_context()
        {
            // Arrange
            var sameDate = new DateTime(2024, 6, 1); // query content is identical for both executions

            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<DatedStreamQuery, string>(
                (q, ctx) => (string)ctx.Bag["route"] == "legacy"
                    ? typeof(LegacyDatedStreamHandler)
                    : typeof(NewDatedStreamHandler),
                typeof(LegacyDatedStreamHandler), typeof(NewDatedStreamHandler));

            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();

            var handlers = new Dictionary<Type, IQueryHandler>
            {
                [typeof(LegacyDatedStreamHandler)] = new LegacyDatedStreamHandler(),
                [typeof(NewDatedStreamHandler)] = new NewDatedStreamHandler()
            };

            var handlerFactory = new SimpleHandlerFactory(type => handlers[type]);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new NotImplementedException());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);

            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            var legacyContext = new QueryContext { Bag = new Dictionary<string, object> { ["route"] = "legacy" } };
            var newContext = new QueryContext { Bag = new Dictionary<string, object> { ["route"] = "new" } };

            // Act — identical queries, different context bag values
            var legacyItems = new List<string>();
            await foreach (var item in queryProcessor.ExecuteStream(new DatedStreamQuery(sameDate), queryContext: legacyContext))
                legacyItems.Add(item);

            var newItems = new List<string>();
            await foreach (var item in queryProcessor.ExecuteStream(new DatedStreamQuery(sameDate), queryContext: newContext))
                newItems.Add(item);

            // Assert — routing dispatches to different stream handlers based on context, not query content
            legacyItems.ShouldBe(new[] { "legacy" });
            newItems.ShouldBe(new[] { "new" });
        }
    }
}
