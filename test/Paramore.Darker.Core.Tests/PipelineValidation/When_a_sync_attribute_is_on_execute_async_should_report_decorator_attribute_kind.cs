using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Policies.Attributes;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class DecoratorAttributeKindRuleTests
    {
        private static readonly ValidationRuleId DecoratorAttributeKind = new("DecoratorAttributeKind");

        [Fact]
        public void When_a_sync_attribute_is_on_execute_async_should_report_decorator_attribute_kind()
        {
            // Arrange — an async handler whose ExecuteAsync carries a sync attribute
            var registry = new QueryHandlerRegistryAsync();
            registry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncHandlerWithSyncAttribute>();
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one error naming the handler, the attribute and the correct kind
            var finding = result.Errors.Single(e => e.RuleId == DecoratorAttributeKind);
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain(nameof(AsyncHandlerWithSyncAttribute));
            finding.Message.ShouldContain(nameof(FallbackPolicyAttribute));
            finding.Message.ShouldContain(nameof(QueryHandlerAttributeAsync));
        }

        [Fact]
        public void When_a_sync_attribute_is_on_execute_async_should_report_decorator_attribute_kind_for_an_async_attribute_on_execute()
        {
            // Arrange — a sync handler whose Execute carries an async attribute
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithAsyncAttribute>();
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — the sync kind is the correct one
            var finding = result.Errors.Single(e => e.RuleId == DecoratorAttributeKind);
            finding.Message.ShouldContain(nameof(SyncHandlerWithAsyncAttribute));
            finding.Message.ShouldContain(nameof(FallbackPolicyAttributeAsync));
            finding.Message.ShouldContain(nameof(QueryHandlerAttribute));
        }

        [Fact]
        public void When_a_sync_attribute_is_on_execute_async_should_report_decorator_attribute_kind_for_a_stream_attribute_on_execute()
        {
            // Arrange — a sync handler whose Execute carries a stream attribute
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithStreamAttribute>();
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert
            var finding = result.Errors.Single(e => e.RuleId == DecoratorAttributeKind);
            finding.Message.ShouldContain(nameof(SyncHandlerWithStreamAttribute));
            finding.Message.ShouldContain(nameof(StreamStepEventAttribute));
            finding.Message.ShouldContain(nameof(QueryHandlerAttribute));
        }

        [Fact]
        public void When_a_sync_attribute_is_on_execute_async_should_report_decorator_attribute_kind_for_a_stream_attribute_on_execute_async()
        {
            // Arrange — an async handler whose ExecuteAsync carries a stream attribute
            var registry = new QueryHandlerRegistryAsync();
            registry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncHandlerWithStreamAttribute>();
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert
            var finding = result.Errors.Single(e => e.RuleId == DecoratorAttributeKind);
            finding.Message.ShouldContain(nameof(AsyncHandlerWithStreamAttribute));
            finding.Message.ShouldContain(nameof(StreamStepEventAttribute));
            finding.Message.ShouldContain(nameof(QueryHandlerAttributeAsync));
        }

        [Fact]
        public void When_a_sync_attribute_is_on_execute_async_should_report_decorator_attribute_kind_for_a_sync_attribute_on_a_stream_handler()
        {
            // Arrange — a stream handler whose ExecuteAsync carries a sync attribute
            var registry = new StreamQueryHandlerRegistry();
            registry.Register<StreamTestQuery, string, StreamHandlerWithSyncAttribute>();
            var pipelineValidator = new PipelineValidatorBuilder().WithStreamRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — the stream kind is the correct one
            var finding = result.Errors.Single(e => e.RuleId == DecoratorAttributeKind);
            finding.Message.ShouldContain(nameof(StreamHandlerWithSyncAttribute));
            finding.Message.ShouldContain(nameof(FallbackPolicyAttribute));
            finding.Message.ShouldContain(nameof(StreamQueryHandlerAttribute));
        }

        [Fact]
        public void When_a_sync_attribute_is_on_execute_async_should_report_decorator_attribute_kind_for_an_async_attribute_on_a_stream_handler()
        {
            // Arrange — a stream handler whose ExecuteAsync carries an async attribute
            var registry = new StreamQueryHandlerRegistry();
            registry.Register<StreamTestQuery, string, StreamHandlerWithAsyncAttribute>();
            var pipelineValidator = new PipelineValidatorBuilder().WithStreamRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert
            var finding = result.Errors.Single(e => e.RuleId == DecoratorAttributeKind);
            finding.Message.ShouldContain(nameof(StreamHandlerWithAsyncAttribute));
            finding.Message.ShouldContain(nameof(FallbackPolicyAttributeAsync));
            finding.Message.ShouldContain(nameof(StreamQueryHandlerAttribute));
        }
    }
}
