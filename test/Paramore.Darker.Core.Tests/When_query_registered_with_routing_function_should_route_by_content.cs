using System;
using System.Collections.Generic;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class SyncRoutingTests
    {
        [Fact]
        public void When_query_registered_with_routing_function_should_route_by_content()
        {
            // Arrange
            var cutover = new DateTime(2024, 1, 1);

            var registry = new QueryHandlerRegistry();
            registry.Register<DatedQuery, string>(
                (q, ctx) => q.Date < cutover ? typeof(LegacyDatedQueryHandler) : typeof(NewDatedQueryHandler),
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

            // Act — same query type, different date fields
            var legacyResult = queryProcessor.Execute(new DatedQuery(new DateTime(2020, 6, 1)));
            var newResult = queryProcessor.Execute(new DatedQuery(new DateTime(2025, 6, 1)));

            // Assert — routing function dispatches to different handlers based on query content
            legacyResult.ShouldBe("legacy");
            newResult.ShouldBe("new");
        }
    }
}
