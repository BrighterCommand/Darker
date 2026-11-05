using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Policies.Attributes;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class DuplicateStepRuleTests
    {
        private static readonly ValidationRuleId DuplicateStep = new("DuplicateStep");

        [Fact]
        public void When_two_decorator_attributes_share_a_step_should_report_duplicate_step_on_the_sync_path()
        {
            // Arrange — a sync handler with two attributes at Step 2
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, DuplicateStepSyncHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one warning naming the handler, the step and both attributes
            var finding = result.Warnings.Single(w => w.RuleId == DuplicateStep);
            finding.Severity.ShouldBe(ValidationSeverity.Warning);
            finding.Message.ShouldContain(nameof(DuplicateStepSyncHandler));
            finding.Message.ShouldContain("2");
            finding.Message.ShouldContain(nameof(FirstPassThroughAttribute));
            finding.Message.ShouldContain(nameof(SecondPassThroughAttribute));
        }

        [Fact]
        public void When_two_decorator_attributes_share_a_step_should_report_duplicate_step_on_the_async_path()
        {
            // Arrange — an async handler with two attributes at Step 2
            var registry = new QueryHandlerRegistryAsync();
            registry.Register<AsyncTestQuery, AsyncTestQuery.Result, DuplicateStepAsyncHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert
            var finding = result.Warnings.Single(w => w.RuleId == DuplicateStep);
            finding.Severity.ShouldBe(ValidationSeverity.Warning);
            finding.Message.ShouldContain(nameof(DuplicateStepAsyncHandler));
            finding.Message.ShouldContain("2");
            finding.Message.ShouldContain(nameof(PassThroughAsyncAttribute));
            finding.Message.ShouldContain(nameof(FallbackPolicyAttributeAsync));
        }

        [Fact]
        public void When_two_decorator_attributes_share_a_step_should_report_duplicate_step_on_the_stream_path()
        {
            // Arrange — a stream handler with two attributes at Step 2
            var registry = new StreamQueryHandlerRegistry();
            registry.Register<StreamTestQuery, string, DuplicateStepStreamHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithStreamRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert
            var finding = result.Warnings.Single(w => w.RuleId == DuplicateStep);
            finding.Severity.ShouldBe(ValidationSeverity.Warning);
            finding.Message.ShouldContain(nameof(DuplicateStepStreamHandler));
            finding.Message.ShouldContain("2");
            finding.Message.ShouldContain(nameof(PassThroughStreamAttribute));
            finding.Message.ShouldContain(nameof(StreamStepEventAttribute));
        }

        [Fact]
        public void When_two_decorator_attributes_share_a_step_should_report_duplicate_step_but_not_for_distinct_steps()
        {
            // Arrange — a sync handler whose attributes sit at Steps 1, 2 and 3
            var registry = new QueryHandlerRegistry();
            registry.Register<StepOrderRecordingQuery, string, StepOrderRecordingQueryHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert
            result.Warnings.ShouldNotContain(w => w.RuleId == DuplicateStep);
        }
    }
}
