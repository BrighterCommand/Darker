using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class AsyncRoutingRegistrationValidationTests
    {
        [Fact]
        public void When_async_routing_candidate_does_not_implement_handler_interface_should_throw_ConfigurationException_at_registration()
        {
            // Arrange
            var asyncRegistry = new QueryHandlerRegistryAsync();

            // Act — ProcessorQueryHandlerAsync implements IQueryHandlerAsync<ProcessorQuery,Guid>, not IQueryHandlerAsync<DatedQuery,string>
            var exception = Assert.Throws<ConfigurationException>(() =>
                asyncRegistry.Register<DatedQuery, string>(
                    (q, ctx) => typeof(LegacyDatedQueryHandlerAsync),
                    typeof(LegacyDatedQueryHandlerAsync),
                    typeof(ProcessorQueryHandlerAsync))); // invalid candidate

            // Assert — registration-time ConfigurationException names the offending type
            exception.Message.ShouldContain(nameof(ProcessorQueryHandlerAsync));
            exception.ShouldNotBeAssignableTo<RoutingException>();
        }

        [Fact]
        public void When_async_routing_registered_for_already_registered_query_type_should_throw_ConfigurationException()
        {
            // Arrange — type-based registration first
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<DatedQuery, string, LegacyDatedQueryHandlerAsync>();

            // Act — routing Register for the same query type
            var exception = Assert.Throws<ConfigurationException>(() =>
                asyncRegistry.Register<DatedQuery, string>(
                    (q, ctx) => typeof(LegacyDatedQueryHandlerAsync),
                    typeof(LegacyDatedQueryHandlerAsync), typeof(NewDatedQueryHandlerAsync)));

            // Assert — duplicate key caught at registration time
            exception.Message.ShouldContain(nameof(DatedQuery));
        }

        [Fact]
        public void When_async_type_based_registered_after_async_routing_registration_should_throw_ConfigurationException()
        {
            // Arrange — routing registration first
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<DatedQuery, string>(
                (q, ctx) => typeof(LegacyDatedQueryHandlerAsync),
                typeof(LegacyDatedQueryHandlerAsync), typeof(NewDatedQueryHandlerAsync));

            // Act — type-based Register for the same query type
            var exception = Assert.Throws<ConfigurationException>(() =>
                asyncRegistry.Register<DatedQuery, string, LegacyDatedQueryHandlerAsync>());

            // Assert — duplicate key caught at registration time
            exception.Message.ShouldContain(nameof(DatedQuery));
        }
    }
}
