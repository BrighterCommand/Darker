using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class RouterWithTwoInternalCandidatesTests
    {
        private static readonly ValidationRuleId HandlerPublic = new("HandlerPublic");

        [Fact]
        public void When_a_router_has_two_internal_candidates_should_report_each_candidate()
        {
            // Arrange — a router with two internal candidates, A (new) then B (legacy)
            var registry = new QueryHandlerRegistry();
            registry.Register<DatedQuery, string>(
                (_, _) => typeof(NewDatedQueryHandler),
                typeof(NewDatedQueryHandler), typeof(LegacyDatedQueryHandler));
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one HandlerPublic error per candidate, in candidate order
            var findings = result.Errors.Where(e => e.RuleId == HandlerPublic).ToArray();
            findings.All(f => f.Severity == ValidationSeverity.Error).ShouldBeTrue();
            findings.Select(f => f.Message.Contains(nameof(NewDatedQueryHandler)) ? nameof(NewDatedQueryHandler)
                    : f.Message.Contains(nameof(LegacyDatedQueryHandler)) ? nameof(LegacyDatedQueryHandler) : "?")
                .ShouldBe([nameof(NewDatedQueryHandler), nameof(LegacyDatedQueryHandler)]);
        }
    }
}
