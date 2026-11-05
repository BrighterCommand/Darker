using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Policies.Handlers;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class GenericDecoratorNameRenderingTests
    {
        private static readonly ValidationRuleId DecoratorResolvable = new("DecoratorResolvable");

        [Fact]
        public void When_a_generic_decorator_is_not_registered_should_name_it_with_open_generic_notation()
        {
            // Arrange — a handler whose generic retry decorator the probe says is not registered
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, RetryableQueryHandler>();
            var probe = new SimpleResolvabilityProbe(type => type != typeof(RetryableQueryDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>));
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).WithProbe(probe).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — the decorator reads RetryableQueryDecorator<,>, not the CLR name RetryableQueryDecorator`2
            var finding = result.Errors.Single(e => e.RuleId == DecoratorResolvable);
            finding.Message.ShouldContain("'RetryableQueryDecorator<,>'");
            finding.Message.ShouldNotContain("`");
        }
    }
}
