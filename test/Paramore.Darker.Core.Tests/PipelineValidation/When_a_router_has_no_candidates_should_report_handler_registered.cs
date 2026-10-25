using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class RouterWithNoCandidatesTests
    {
        private static readonly ValidationRuleId HandlerRegistered = new("HandlerRegistered");

        [Fact]
        public void When_a_router_has_no_candidates_should_report_handler_registered()
        {
            // Arrange — a router registration supplying no candidate handler types
            var registry = new QueryHandlerRegistry();
            registry.Register<TestQuery, TestQuery.Result>((q, c) => null);
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one error naming the query
            var findings = result.Errors.Where(e => e.RuleId == HandlerRegistered).ToArray();
            findings.Length.ShouldBe(1);
            findings[0].Severity.ShouldBe(ValidationSeverity.Error);
            findings[0].Message.ShouldContain(nameof(TestQuery));
        }
    }
}
