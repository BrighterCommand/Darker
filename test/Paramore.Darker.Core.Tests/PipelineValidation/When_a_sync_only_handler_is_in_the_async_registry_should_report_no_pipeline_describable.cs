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
    public class SyncOnlyHandlerInAsyncRegistryPipelineDescribableTests
    {
        private static readonly ValidationRuleId PipelineDescribable = new("PipelineDescribable");
        private static readonly ValidationRuleId HandlerMatchesPath = new("HandlerMatchesPath");

        [Fact]
        public void When_a_sync_only_handler_is_in_the_async_registry_should_report_no_pipeline_describable()
        {
            // Arrange — a sync-only handler registered on the async registry: the path method is absent, not unreadable
            var registry = new QueryHandlerRegistryAsync();
            registry.Register(typeof(TestQueryA), typeof(Guid), typeof(TestQueryHandler));
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — PipelineDescribable is an Error rule; no such finding, but HandlerMatchesPath is reported
            result.Errors.Any(e => e.RuleId == PipelineDescribable).ShouldBeFalse();
            result.Errors.Any(e => e.RuleId == HandlerMatchesPath).ShouldBeTrue();
        }
    }
}
