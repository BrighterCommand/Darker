using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class NoHandlerTypeDecoratorResolvabilityTests
    {
        private static readonly ValidationRuleId DecoratorResolvable = new("DecoratorResolvable");
        private static readonly ValidationRuleId RuleEvaluationFailed = new("RuleEvaluationFailed");

        [Fact]
        public void When_a_description_has_no_handler_type_should_report_no_decorator_resolvable()
        {
            // Arrange — a registration with a null handler type and a probe that answers false to everything
            var registry = new QueryHandlerRegistry();
            registry.Register(typeof(TestQuery), typeof(TestQuery.Result), null!);
            var probe = new SimpleResolvabilityProbe(_ => false);
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).WithProbe(probe).Build();

            // Act
            PipelineValidationResult? validation = null;
            var thrown = Record.Exception(() => validation = pipelineValidator.Validate());

            // Assert — validation does not throw, and there are no decorator steps to report on
            thrown.ShouldBeNull();
            validation!.Errors.Where(e => e.RuleId == DecoratorResolvable).ShouldBeEmpty();
            validation.Errors.Where(e => e.RuleId == RuleEvaluationFailed).ShouldBeEmpty();
        }
    }
}
