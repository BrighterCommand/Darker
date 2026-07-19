using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class AsyncContextRoutingTests
    {
        [Fact]
        public async Task When_async_routing_function_reads_context_should_route_by_context()
        {
            // Arrange
            var sameDate = new DateTime(2024, 6, 1); // query content is identical for both executions

            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<DatedQuery, string>(
                (q, ctx) => (string)ctx.Bag["route"] == "legacy"
                    ? typeof(LegacyDatedQueryHandlerAsync)
                    : typeof(NewDatedQueryHandlerAsync),
                typeof(LegacyDatedQueryHandlerAsync), typeof(NewDatedQueryHandlerAsync));

            var handlers = new Dictionary<Type, IQueryHandler>
            {
                [typeof(LegacyDatedQueryHandlerAsync)] = new LegacyDatedQueryHandlerAsync(),
                [typeof(NewDatedQueryHandlerAsync)] = new NewDatedQueryHandlerAsync()
            };

            var handlerFactory = new SimpleHandlerFactory(type => handlers[type]);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new NotImplementedException());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            var legacyContext = new QueryContext { Bag = new Dictionary<string, object> { ["route"] = "legacy" } };
            var newContext = new QueryContext { Bag = new Dictionary<string, object> { ["route"] = "new" } };

            // Act — identical queries, different context bag values
            var legacyResult = await queryProcessor.ExecuteAsync(new DatedQuery(sameDate), queryContext: legacyContext);
            var newResult = await queryProcessor.ExecuteAsync(new DatedQuery(sameDate), queryContext: newContext);

            // Assert — routing dispatches to different async handlers based on context, not query content
            legacyResult.ShouldBe("legacy");
            newResult.ShouldBe("new");
        }
    }
}
