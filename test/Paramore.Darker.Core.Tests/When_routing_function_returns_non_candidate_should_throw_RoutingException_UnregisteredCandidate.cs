using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class RoutingNonCandidateTests
    {
        [Fact]
        public void When_routing_function_returns_non_candidate_should_throw_RoutingException_UnregisteredCandidate()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<DatedQuery, string>(
                (q, ctx) => typeof(ProcessorQueryHandler), // returns a type NOT in the declared candidate set
                typeof(LegacyDatedQueryHandler), typeof(NewDatedQueryHandler));

            var handlerFactory = new SimpleHandlerFactory(_ => throw new NotImplementedException());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new NotImplementedException());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(registry, handlerFactory, decoratorRegistry, decoratorFactory);
            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // Act
            var exception = Assert.Throws<RoutingException>(
                () => queryProcessor.Execute(new DatedQuery(new DateTime(2024, 1, 1))));

            // Assert — non-candidate return gives RoutingException.UnregisteredCandidate naming the resolved type
            exception.Reason.ShouldBe(RoutingFailure.UnregisteredCandidate);
            exception.Message.ShouldContain(nameof(ProcessorQueryHandler));
        }
    }
}
