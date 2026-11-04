using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class OrderingRuleDecoratorNameRenderingTests
    {
        private static readonly ValidationRuleId CustomOrdering = new("CustomOrdering");

        [Fact]
        public void When_an_ordering_rule_is_violated_should_name_its_decorators_with_open_generic_notation()
        {
            // Arrange — a custom pair of open generic decorators that the handler places the wrong way round
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, CustomAndFallbackOrderingViolationHandler>();
            var customRule = new DecoratorOrderingRule(typeof(OuterDouble<,>), typeof(InnerDouble<,>), CustomOrdering, "the inner must be wrapped by the outer");
            var pipelineValidator = new PipelineValidatorBuilder()
                .WithSyncRegistry(registry)
                .WithOrderingRules(DecoratorOrderingRules.Default.Append(customRule))
                .Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — both decorators read Name<,>, in the message's quotes
            var finding = result.Warnings.Single(w => w.RuleId == CustomOrdering);
            finding.Message.ShouldContain("'OuterDouble<,>'");
            finding.Message.ShouldContain("'InnerDouble<,>'");
        }
    }
}
