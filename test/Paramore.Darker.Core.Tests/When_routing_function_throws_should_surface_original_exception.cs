using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class RoutingFunctionThrowsTests
    {
        [Fact]
        public void When_routing_function_throws_should_surface_original_exception_not_wrapped()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<DatedQuery, string>(
                (q, ctx) => throw new InvalidOperationException("boom"), // router itself throws
                typeof(LegacyDatedQueryHandler), typeof(NewDatedQueryHandler));

            var handlerFactory = new SimpleHandlerFactory(_ => throw new NotImplementedException());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new NotImplementedException());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(registry, handlerFactory, decoratorRegistry, decoratorFactory);
            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // Act
            var exception = Assert.Throws<InvalidOperationException>(
                () => queryProcessor.Execute(new DatedQuery(new DateTime(2024, 1, 1))));

            // Assert — original exception surfaces unchanged; not wrapped in RoutingException or ConfigurationException
            exception.Message.ShouldBe("boom");
            exception.ShouldNotBeAssignableTo<RoutingException>();
            exception.ShouldNotBeAssignableTo<ConfigurationException>();
        }
    }
}
