using System;
using System.Linq;
using Paramore.Darker.Core.Tests.Exported;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class HandlerResolvableRuleTests
    {
        private static readonly ValidationRuleId HandlerResolvable = new("HandlerResolvable");

        [Fact]
        public void When_the_probe_says_a_handler_is_not_registered_should_report_handler_resolvable()
        {
            // Arrange — a public handler that the probe says is not registered
            var registry = new QueryHandlerRegistry();
            registry.Register<TestQueryA, Guid, TestQueryHandler>();
            var probe = new SimpleResolvabilityProbe(_ => false);
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).WithProbe(probe).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one error naming the handler and advising to register it
            var findings = result.Errors.Where(e => e.RuleId == HandlerResolvable).ToArray();
            findings.Length.ShouldBe(1);
            findings[0].Severity.ShouldBe(ValidationSeverity.Error);
            findings[0].Message.ShouldContain(nameof(TestQueryHandler));
            findings[0].Message.ShouldContain("Register");
        }

        [Fact]
        public void When_the_probe_says_a_handler_is_registered_should_report_no_handler_resolvable()
        {
            // Arrange — a public handler that the probe says is registered
            var registry = new QueryHandlerRegistry();
            registry.Register<TestQueryA, Guid, TestQueryHandler>();
            var probe = new SimpleResolvabilityProbe(_ => true);
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).WithProbe(probe).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert
            result.Errors.Where(e => e.RuleId == HandlerResolvable).ShouldBeEmpty();
        }
    }
}
