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
    public class ProbeFailureTests
    {
        private const string ProbeFailureMessage = "the container is unavailable";
        private static readonly ValidationRuleId RuleEvaluationFailed = new("RuleEvaluationFailed");

        [Fact]
        public void When_the_probe_throws_should_report_rule_evaluation_failed()
        {
            // Arrange — a public handler and a probe that throws
            var registry = new QueryHandlerRegistry();
            registry.Register<TestQueryA, Guid, TestQueryHandler>();
            var probe = new SimpleResolvabilityProbe(_ => throw new InvalidOperationException(ProbeFailureMessage));
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).WithProbe(probe).Build();

            // Act — validation reports the failure instead of throwing
            var result = pipelineValidator.Validate();

            // Assert — an error naming the entity and carrying the exception message
            var findings = result.Errors.Where(e => e.RuleId == RuleEvaluationFailed).ToArray();
            findings.Length.ShouldBe(1);
            findings[0].Severity.ShouldBe(ValidationSeverity.Error);
            findings[0].Source.ShouldContain(nameof(TestQueryHandler));
            findings[0].Message.ShouldContain(ProbeFailureMessage);
        }
    }
}
