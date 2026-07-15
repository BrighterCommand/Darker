using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class RoutingDuplicateRegistrationTests
    {
        [Fact]
        public void When_routing_registered_for_already_registered_query_type_should_throw_ConfigurationException()
        {
            // Arrange — type-based registration first
            var registry = new QueryHandlerRegistry();
            registry.Register<DatedQuery, string, LegacyDatedQueryHandler>();

            // Act — routing Register for the same query type
            var exception = Assert.Throws<ConfigurationException>(() =>
                registry.Register<DatedQuery, string>(
                    (q, ctx) => typeof(LegacyDatedQueryHandler),
                    typeof(LegacyDatedQueryHandler), typeof(NewDatedQueryHandler)));

            // Assert — duplicate key caught at registration time
            exception.Message.ShouldContain(nameof(DatedQuery));
        }

        [Fact]
        public void When_type_based_registered_after_routing_registration_should_throw_ConfigurationException()
        {
            // Arrange — routing registration first
            var registry = new QueryHandlerRegistry();
            registry.Register<DatedQuery, string>(
                (q, ctx) => typeof(LegacyDatedQueryHandler),
                typeof(LegacyDatedQueryHandler), typeof(NewDatedQueryHandler));

            // Act — type-based Register for the same query type
            var exception = Assert.Throws<ConfigurationException>(() =>
                registry.Register<DatedQuery, string, LegacyDatedQueryHandler>());

            // Assert — duplicate key caught at registration time
            exception.Message.ShouldContain(nameof(DatedQuery));
        }
    }
}
