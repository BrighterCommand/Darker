using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class RoutingExceptionTests
    {
        [Fact]
        public void When_routing_exception_constructed_with_no_handler_resolved_should_expose_reason_and_name_query_type()
        {
            // Arrange
            var queryType = typeof(SomeQuery);

            // Act
            var exception = new RoutingException(RoutingFailure.NoHandlerResolved, queryType);

            // Assert
            exception.Reason.ShouldBe(RoutingFailure.NoHandlerResolved);
            exception.Message.ShouldContain(nameof(SomeQuery));
        }

        [Fact]
        public void When_routing_exception_constructed_with_unregistered_candidate_should_expose_reason_and_name_handler_type()
        {
            // Arrange
            var queryType = typeof(SomeQuery);
            var handlerType = typeof(ProcessorQueryHandler);

            // Act
            var exception = new RoutingException(RoutingFailure.UnregisteredCandidate, queryType, handlerType);

            // Assert
            exception.Reason.ShouldBe(RoutingFailure.UnregisteredCandidate);
            exception.Message.ShouldContain(nameof(ProcessorQueryHandler));
        }

        [Fact]
        public void When_routing_exception_constructed_messages_should_differ_by_failure_mode()
        {
            // Arrange
            var queryType = typeof(SomeQuery);
            var handlerType = typeof(ProcessorQueryHandler);

            // Act
            var noHandlerException = new RoutingException(RoutingFailure.NoHandlerResolved, queryType);
            var unregisteredException = new RoutingException(RoutingFailure.UnregisteredCandidate, queryType, handlerType);

            // Assert — two failure modes produce distinct messages
            noHandlerException.Message.ShouldNotBe(unregisteredException.Message);
        }

        [Fact]
        public void When_routing_exception_thrown_should_not_be_caught_by_configuration_exception_handler()
        {
            // Arrange
            var exception = new RoutingException(RoutingFailure.NoHandlerResolved, typeof(SomeQuery));

            // Assert — RoutingException derives from Exception, not ConfigurationException
            exception.ShouldBeAssignableTo<Exception>();
            exception.ShouldNotBeAssignableTo<ConfigurationException>();
        }
    }
}
