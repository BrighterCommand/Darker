using System;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class AsyncRoutingNonCandidateTests
    {
        [Fact]
        public async Task When_async_routing_function_returns_non_candidate_should_throw_RoutingException_UnregisteredCandidate()
        {
            // Arrange
            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<DatedQuery, string>(
                (q, ctx) => typeof(ProcessorQueryHandler), // returns a type NOT in the declared candidate set
                typeof(LegacyDatedQueryHandlerAsync), typeof(NewDatedQueryHandlerAsync));

            var handlerFactory = new SimpleHandlerFactory(_ => throw new NotImplementedException());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new NotImplementedException());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // Act
            var exception = await Assert.ThrowsAsync<RoutingException>(
                () => queryProcessor.ExecuteAsync(new DatedQuery(new DateTime(2024, 1, 1))));

            // Assert — non-candidate return gives RoutingException.UnregisteredCandidate naming the resolved type
            exception.Reason.ShouldBe(RoutingFailure.UnregisteredCandidate);
            exception.Message.ShouldContain(nameof(ProcessorQueryHandler));
        }
    }
}
