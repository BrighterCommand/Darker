using System;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class StreamRoutingNullReturnTests
    {
        [Fact]
        public async Task When_stream_routing_function_returns_null_should_throw_RoutingException_NoHandlerResolved()
        {
            // Arrange
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<DatedStreamQuery, string>(
                (q, ctx) => null, // router always returns null — simulates no match
                typeof(LegacyDatedStreamHandler), typeof(NewDatedStreamHandler));

            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            var handlerFactory = new SimpleHandlerFactory(_ => throw new NotImplementedException());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new NotImplementedException());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);

            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // Act — exception surfaces on first MoveNextAsync, before any item is yielded
            RoutingException caughtException = null;
            try
            {
                await foreach (var item in queryProcessor.ExecuteStream(new DatedStreamQuery(new DateTime(2024, 1, 1))))
                {
                    // should not reach here
                }
            }
            catch (RoutingException ex)
            {
                caughtException = ex;
            }

            // Assert — null from router gives RoutingException.NoHandlerResolved, not ConfigurationException
            caughtException.ShouldNotBeNull();
            caughtException.Reason.ShouldBe(RoutingFailure.NoHandlerResolved);
            caughtException.ShouldNotBeAssignableTo<ConfigurationException>();
        }
    }
}
