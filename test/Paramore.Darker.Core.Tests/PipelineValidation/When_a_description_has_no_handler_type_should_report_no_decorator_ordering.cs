using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class NoHandlerTypeDecoratorOrderingTests
    {
        private static readonly ValidationRuleId DecoratorOrdering = new("DecoratorOrdering");
        private static readonly ValidationRuleId RuleEvaluationFailed = new("RuleEvaluationFailed");

        [Fact]
        public void When_a_description_has_no_handler_type_should_report_no_decorator_ordering()
        {
            // Arrange — registrations with a null handler type on both the sync and async registries
            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register(typeof(TestQuery), typeof(TestQuery.Result), null!);
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register(typeof(TestQuery), typeof(TestQuery.Result), null!);
            var pipelineValidator = new PipelineValidatorBuilder()
                .WithSyncRegistry(syncRegistry)
                .WithAsyncRegistry(asyncRegistry)
                .Build();

            // Act
            PipelineValidationResult? validation = null;
            var thrown = Record.Exception(() => validation = pipelineValidator.Validate());

            // Assert — validation does not throw, and there are no steps to order
            thrown.ShouldBeNull();
            validation!.Warnings.Where(w => w.RuleId == DecoratorOrdering).ShouldBeEmpty();
            validation.Errors.Where(e => e.RuleId == DecoratorOrdering).ShouldBeEmpty();
            validation.Errors.Where(e => e.RuleId == RuleEvaluationFailed).ShouldBeEmpty();
        }
    }
}
