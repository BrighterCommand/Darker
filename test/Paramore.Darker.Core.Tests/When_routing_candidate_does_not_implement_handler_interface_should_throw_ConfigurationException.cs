using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class RoutingCandidateValidationTests
    {
        [Fact]
        public void When_routing_candidate_does_not_implement_handler_interface_should_throw_ConfigurationException_at_registration()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();

            // Act — ProcessorQueryHandler implements IQueryHandler<ProcessorQuery,Guid>, not IQueryHandler<DatedQuery,string>
            var exception = Assert.Throws<ConfigurationException>(() =>
                registry.Register<DatedQuery, string>(
                    (q, ctx) => typeof(LegacyDatedQueryHandler),
                    typeof(LegacyDatedQueryHandler),
                    typeof(ProcessorQueryHandler))); // invalid candidate

            // Assert — registration-time ConfigurationException names the offending type
            exception.Message.ShouldContain(nameof(ProcessorQueryHandler));
            exception.ShouldNotBeAssignableTo<RoutingException>();
        }
    }
}
