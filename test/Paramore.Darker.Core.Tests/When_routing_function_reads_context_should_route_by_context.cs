using System;
using System.Collections.Generic;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class SyncContextRoutingTests
    {
        [Fact]
        public void When_routing_function_reads_context_should_route_by_context()
        {
            // Arrange
            var sameDate = new DateTime(2024, 6, 1); // query content is identical for both executions

            var registry = new QueryHandlerRegistry();
            registry.Register<DatedQuery, string>(
                (q, ctx) => (string)ctx.Bag["route"] == "legacy"
                    ? typeof(LegacyDatedQueryHandler)
                    : typeof(NewDatedQueryHandler),
                typeof(LegacyDatedQueryHandler), typeof(NewDatedQueryHandler));

            var handlers = new Dictionary<Type, IQueryHandler>
            {
                [typeof(LegacyDatedQueryHandler)] = new LegacyDatedQueryHandler(),
                [typeof(NewDatedQueryHandler)] = new NewDatedQueryHandler()
            };

            var handlerFactory = new SimpleHandlerFactory(type => handlers[type]);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new NotImplementedException());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(registry, handlerFactory, decoratorRegistry, decoratorFactory);
            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            var legacyContext = new QueryContext { Bag = new Dictionary<string, object> { ["route"] = "legacy" } };
            var newContext = new QueryContext { Bag = new Dictionary<string, object> { ["route"] = "new" } };

            // Act — identical queries, different context bag values
            var legacyResult = queryProcessor.Execute(new DatedQuery(sameDate), queryContext: legacyContext);
            var newResult = queryProcessor.Execute(new DatedQuery(sameDate), queryContext: newContext);

            // Assert — routing dispatches to different handlers based on context, not query content
            legacyResult.ShouldBe("legacy");
            newResult.ShouldBe("new");
        }
    }
}
