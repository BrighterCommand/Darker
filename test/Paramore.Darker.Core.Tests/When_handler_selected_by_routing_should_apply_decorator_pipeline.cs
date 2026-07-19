using System;
using System.Collections.Generic;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class RoutingDecoratorPipelineTests
    {
        [Fact]
        public void When_handler_selected_by_routing_should_apply_decorator_pipeline()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<DatedQuery, string>(
                (q, ctx) => typeof(AppendSuffixDatedQueryHandler),
                typeof(AppendSuffixDatedQueryHandler));

            var handlers = new Dictionary<Type, IQueryHandler>
            {
                [typeof(AppendSuffixDatedQueryHandler)] = new AppendSuffixDatedQueryHandler()
            };
            var handlerFactory = new SimpleHandlerFactory(type => handlers[type]);

            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                new AppendSuffixDecorator<IQuery<string>, string>());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(registry, handlerFactory, decoratorRegistry, decoratorFactory);
            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // Act
            var result = queryProcessor.Execute(new DatedQuery(new DateTime(2024, 1, 1)));

            // Assert — "-decorated" suffix proves the decorator pipeline was traversed, not bypassed
            result.ShouldBe("routed-decorated");
        }
    }
}
