using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class StreamRoutingTests
    {
        [Fact]
        public async Task When_stream_query_registered_with_routing_function_should_route_by_content()
        {
            // Arrange
            var cutover = new DateTime(2024, 1, 1);

            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<DatedStreamQuery, string>(
                (q, ctx) => q.Date < cutover ? typeof(LegacyDatedStreamHandler) : typeof(NewDatedStreamHandler),
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

            // Act — same stream query type, different date fields
            var legacyItems = new List<string>();
            await foreach (var item in queryProcessor.ExecuteStream(new DatedStreamQuery(new DateTime(2020, 6, 1))))
                legacyItems.Add(item);

            var newItems = new List<string>();
            await foreach (var item in queryProcessor.ExecuteStream(new DatedStreamQuery(new DateTime(2025, 6, 1))))
                newItems.Add(item);

            // Assert — routing function dispatches to different stream handlers based on query content
            legacyItems.ShouldBe(new[] { "legacy" });
            newItems.ShouldBe(new[] { "new" });
        }
    }
}
