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
    public class ResolvabilityNotCheckedTests
    {
        private static readonly ValidationRuleId ResolvabilityNotChecked = new("ResolvabilityNotChecked");
        private static readonly ValidationRuleId HandlerResolvable = new("HandlerResolvable");
        private static readonly ValidationRuleId DecoratorResolvable = new("DecoratorResolvable");

        [Fact]
        public void When_validating_without_a_probe_should_report_resolvability_not_checked()
        {
            // Arrange — a clean configuration across three registries, and no probe
            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<TestQueryA, Guid, TestQueryHandler>();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<TestQueryA, Guid, TestQueryHandlerAsync>();
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<ExportedStreamQuery, string, ExportedStreamQueryHandler>();

            var pipelineValidator = new PipelineValidatorBuilder()
                .WithSyncRegistry(syncRegistry)
                .WithAsyncRegistry(asyncRegistry)
                .WithStreamRegistry(streamRegistry)
                .Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert
            result.Warnings.Count(w => w.RuleId == ResolvabilityNotChecked).ShouldBe(1);
            var warning = result.Warnings[0];
            warning.RuleId.ShouldBe(ResolvabilityNotChecked);
            warning.Severity.ShouldBe(ValidationSeverity.Warning);
            warning.Message.ShouldContain("probe");
            result.Errors.Any(e => e.RuleId == HandlerResolvable || e.RuleId == DecoratorResolvable).ShouldBeFalse();
            result.Warnings.Any(w => w.RuleId == HandlerResolvable || w.RuleId == DecoratorResolvable).ShouldBeFalse();
            result.IsValid.ShouldBeTrue();
        }
    }
}
