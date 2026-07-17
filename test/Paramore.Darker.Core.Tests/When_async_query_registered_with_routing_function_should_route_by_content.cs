using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class AsyncRoutingTests
    {
        [Fact]
        public async Task When_async_query_registered_with_routing_function_should_route_by_content()
        {
            // Arrange
            var cutover = new DateTime(2024, 1, 1);

            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<DatedQuery, string>(
                (q, ctx) => q.Date < cutover ? typeof(LegacyDatedQueryHandlerAsync) : typeof(NewDatedQueryHandlerAsync),
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

            // Act — same query type, different date fields
            var legacyResult = await queryProcessor.ExecuteAsync(new DatedQuery(new DateTime(2020, 6, 1)));
            var newResult = await queryProcessor.ExecuteAsync(new DatedQuery(new DateTime(2025, 6, 1)));

            // Assert — routing function dispatches to different async handlers based on query content
            legacyResult.ShouldBe("legacy");
            newResult.ShouldBe("new");
        }
    }
}
