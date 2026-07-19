using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class StreamRoutingRegistrationValidationTests
    {
        [Fact]
        public void When_stream_routing_candidate_does_not_implement_handler_interface_should_throw_ConfigurationException_at_registration()
        {
            // Arrange
            var streamRegistry = new StreamQueryHandlerRegistry();

            // Act — MultiItemStreamHandler implements IStreamQueryHandler<MultiItemStreamQuery,string>, not IStreamQueryHandler<DatedStreamQuery,string>
            var exception = Assert.Throws<ConfigurationException>(() =>
                streamRegistry.Register<DatedStreamQuery, string>(
                    (q, ctx) => typeof(LegacyDatedStreamHandler),
                    typeof(LegacyDatedStreamHandler),
                    typeof(MultiItemStreamHandler))); // invalid candidate

            // Assert — registration-time ConfigurationException names the offending type
            exception.Message.ShouldContain(nameof(MultiItemStreamHandler));
            exception.ShouldNotBeAssignableTo<RoutingException>();
        }

        [Fact]
        public void When_stream_routing_registered_for_already_registered_query_type_should_throw_ConfigurationException()
        {
            // Arrange — type-based registration first
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<DatedStreamQuery, string, LegacyDatedStreamHandler>();

            // Act — routing Register for the same query type
            var exception = Assert.Throws<ConfigurationException>(() =>
                streamRegistry.Register<DatedStreamQuery, string>(
                    (q, ctx) => typeof(LegacyDatedStreamHandler),
                    typeof(LegacyDatedStreamHandler), typeof(NewDatedStreamHandler)));

            // Assert — duplicate key caught at registration time
            exception.Message.ShouldContain(nameof(DatedStreamQuery));
        }

        [Fact]
        public void When_stream_type_based_registered_after_stream_routing_registration_should_throw_ConfigurationException()
        {
            // Arrange — routing registration first
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<DatedStreamQuery, string>(
                (q, ctx) => typeof(LegacyDatedStreamHandler),
                typeof(LegacyDatedStreamHandler), typeof(NewDatedStreamHandler));

            // Act — type-based Register for the same query type
            var exception = Assert.Throws<ConfigurationException>(() =>
                streamRegistry.Register<DatedStreamQuery, string, LegacyDatedStreamHandler>());

            // Assert — duplicate key caught at registration time
            exception.Message.ShouldContain(nameof(DatedStreamQuery));
        }
    }
}
