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
    public class HandlerMatchesPathRuleTests
    {
        private static readonly ValidationRuleId HandlerMatchesPath = new("HandlerMatchesPath");

        [Fact]
        public void When_a_sync_only_handler_is_in_the_async_registry_should_report_handler_matches_path()
        {
            // Arrange — a sync-only handler registered on the async registry
            var registry = new QueryHandlerRegistryAsync();
            registry.Register(typeof(TestQueryA), typeof(Guid), typeof(TestQueryHandler));
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one error naming the handler, the query and the expected interface
            var finding = result.Errors.Single(e => e.RuleId == HandlerMatchesPath);
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain(nameof(TestQueryHandler));
            finding.Message.ShouldContain(nameof(TestQueryA));
            finding.Message.ShouldContain("IQueryHandlerAsync<,>");
        }

        [Fact]
        public void When_a_sync_only_handler_is_in_the_async_registry_should_report_handler_matches_path_for_an_async_handler_in_the_sync_registry()
        {
            // Arrange — an async-only handler registered on the sync registry
            var registry = new QueryHandlerRegistry();
            registry.Register(typeof(TestQueryA), typeof(Guid), typeof(TestQueryHandlerAsync));
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — the sync interface is expected
            var finding = result.Errors.Single(e => e.RuleId == HandlerMatchesPath);
            finding.Message.ShouldContain(nameof(TestQueryHandlerAsync));
            finding.Message.ShouldContain(nameof(TestQueryA));
            finding.Message.ShouldContain("IQueryHandler<,>");
        }

        [Fact]
        public void When_a_sync_only_handler_is_in_the_async_registry_should_report_handler_matches_path_for_a_non_stream_handler_in_the_stream_registry()
        {
            // Arrange — a non-stream handler registered on the stream registry
            var registry = new StreamQueryHandlerRegistry();
            registry.Register(typeof(ExportedStreamQuery), typeof(string), typeof(TestQueryHandlerAsync));
            var pipelineValidator = new PipelineValidatorBuilder().WithStreamRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — the stream interface is expected
            var finding = result.Errors.Single(e => e.RuleId == HandlerMatchesPath);
            finding.Message.ShouldContain(nameof(TestQueryHandlerAsync));
            finding.Message.ShouldContain(nameof(ExportedStreamQuery));
            finding.Message.ShouldContain("IStreamQueryHandler<,>");
        }
    }
}
