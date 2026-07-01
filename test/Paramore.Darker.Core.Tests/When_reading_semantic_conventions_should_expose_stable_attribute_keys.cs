using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_reading_semantic_conventions_should_expose_stable_attribute_keys
    {
        [Fact]
        public void Should_expose_stable_attribute_keys()
        {
            // Arrange — nothing to arrange; constants are static and stateless

            // Act — read all public constant fields from the conventions class
            var sourceName = DarkerSemanticConventions.SourceName;
            var queryId = DarkerSemanticConventions.QueryId;
            var queryType = DarkerSemanticConventions.QueryType;
            var operation = DarkerSemanticConventions.Operation;
            var queryBody = DarkerSemanticConventions.QueryBody;
            var spanContextPrefix = DarkerSemanticConventions.SpanContextPrefix;
            var handlerName = DarkerSemanticConventions.HandlerName;
            var handlerType = DarkerSemanticConventions.HandlerType;
            var isSink = DarkerSemanticConventions.IsSink;
            var errorType = DarkerSemanticConventions.ErrorType;

            // Assert — every key matches the documented string value so a typo cannot silently break tracing
            sourceName.ShouldBe("paramore.darker");
            queryId.ShouldBe("paramore.darker.queryid");
            queryType.ShouldBe("paramore.darker.querytype");
            operation.ShouldBe("paramore.darker.operation");
            queryBody.ShouldBe("paramore.darker.query_body");
            spanContextPrefix.ShouldBe("spancontext.");
            handlerName.ShouldBe("paramore.darker.handlername");
            handlerType.ShouldBe("paramore.darker.handlertype");
            isSink.ShouldBe("paramore.darker.is_sink");
            errorType.ShouldBe("error.type");
        }
    }
}
