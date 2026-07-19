using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class RoutingNullReturnTests
    {
        [Fact]
        public void When_routing_function_returns_null_should_throw_RoutingException_NoHandlerResolved()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<DatedQuery, string>(
                (q, ctx) => null, // router always returns null — simulates no match
                typeof(LegacyDatedQueryHandler), typeof(NewDatedQueryHandler));

            var handlerFactory = new SimpleHandlerFactory(_ => throw new NotImplementedException());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new NotImplementedException());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(registry, handlerFactory, decoratorRegistry, decoratorFactory);
            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // Act
            var exception = Assert.Throws<RoutingException>(
                () => queryProcessor.Execute(new DatedQuery(new DateTime(2024, 1, 1))));

            // Assert — null from router gives RoutingException.NoHandlerResolved, not ConfigurationException
            exception.Reason.ShouldBe(RoutingFailure.NoHandlerResolved);
            exception.ShouldNotBeAssignableTo<ConfigurationException>();
        }
    }
}
