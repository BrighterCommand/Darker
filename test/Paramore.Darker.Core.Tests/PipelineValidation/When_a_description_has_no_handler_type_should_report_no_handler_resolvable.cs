using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class NoHandlerTypeResolvabilityTests
    {
        private static readonly ValidationRuleId HandlerResolvable = new("HandlerResolvable");
        private static readonly ValidationRuleId HandlerRegistered = new("HandlerRegistered");
        private static readonly ValidationRuleId RuleEvaluationFailed = new("RuleEvaluationFailed");

        [Fact]
        public void When_a_description_has_no_handler_type_should_report_no_handler_resolvable()
        {
            // Arrange — a registration with a null handler type and a probe that answers false to everything
            var registry = new QueryHandlerRegistry();
            registry.Register(typeof(TestQuery), typeof(TestQuery.Result), null!);
            var probe = new SimpleResolvabilityProbe(_ => false);
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).WithProbe(probe).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — no resolvability finding, the probe was never asked about null, HandlerRegistered still reported
            result.Errors.Where(e => e.RuleId == HandlerResolvable).ShouldBeEmpty();
            result.Errors.Where(e => e.RuleId == RuleEvaluationFailed).ShouldBeEmpty();
            probe.AskedAbout.ShouldNotContain((System.Type?)null);
            result.Errors.Where(e => e.RuleId == HandlerRegistered).ShouldHaveSingleItem();
        }
    }
}
