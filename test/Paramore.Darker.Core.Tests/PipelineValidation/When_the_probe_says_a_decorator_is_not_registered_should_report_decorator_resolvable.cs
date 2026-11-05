using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Policies.Handlers;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class DecoratorResolvableRuleTests
    {
        private static readonly ValidationRuleId DecoratorResolvable = new("DecoratorResolvable");

        [Fact]
        public void When_the_probe_says_a_decorator_is_not_registered_should_report_decorator_resolvable()
        {
            // Arrange — a handler with a retry decorator that the probe says is not registered
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, RetryableQueryHandler>();
            var probe = new SimpleResolvabilityProbe(type => type != typeof(RetryableQueryDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>));
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).WithProbe(probe).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one error naming the decorator and the handler, advising to register it
            var findings = result.Errors.Where(e => e.RuleId == DecoratorResolvable).ToArray();
            findings.Length.ShouldBe(1);
            findings[0].Severity.ShouldBe(ValidationSeverity.Error);
            findings[0].Message.ShouldContain("RetryableQueryDecorator");
            findings[0].Message.ShouldContain(nameof(RetryableQueryHandler));
            findings[0].Message.ShouldContain("Register");
        }

        [Fact]
        public void When_validating_should_ask_the_probe_about_the_decorator_closed_over_the_query_interface()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, RetryableQueryHandler>();
            var probe = new SimpleResolvabilityProbe(_ => true);
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).WithProbe(probe).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — asked about the IQuery<TResult> closing, not the concrete query type
            probe.AskedAbout.ShouldContain(typeof(RetryableQueryDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>));
            probe.AskedAbout.ShouldNotContain(typeof(RetryableQueryDecorator<SyncTestQuery, SyncTestQuery.Result>));
            result.Errors.Where(e => e.RuleId == DecoratorResolvable).ShouldBeEmpty();
        }
    }
}
