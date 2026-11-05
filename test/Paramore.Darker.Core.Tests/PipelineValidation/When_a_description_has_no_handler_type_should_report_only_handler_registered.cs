using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class NoHandlerTypeTests
    {
        private static readonly ValidationRuleId HandlerRegistered = new("HandlerRegistered");

        [Fact]
        public void When_a_description_has_no_handler_type_should_report_only_handler_registered_on_the_sync_path()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register(typeof(SyncTestQuery), typeof(SyncTestQuery.Result), null!);
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = Should.NotThrow(() => pipelineValidator.Validate());

            // Assert — the single error is HandlerRegistered
            result.Errors.Count.ShouldBe(1);
            result.Errors[0].RuleId.ShouldBe(HandlerRegistered);
        }

        [Fact]
        public void When_a_description_has_no_handler_type_should_report_only_handler_registered_on_the_async_path()
        {
            // Arrange
            var registry = new QueryHandlerRegistryAsync();
            registry.Register(typeof(AsyncTestQuery), typeof(AsyncTestQuery.Result), null!);
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            var result = Should.NotThrow(() => pipelineValidator.Validate());

            // Assert — the single error is HandlerRegistered
            result.Errors.Count.ShouldBe(1);
            result.Errors[0].RuleId.ShouldBe(HandlerRegistered);
        }

        [Fact]
        public void When_a_description_has_no_handler_type_should_report_only_handler_registered_on_the_stream_path()
        {
            // Arrange
            var registry = new StreamQueryHandlerRegistry();
            registry.Register(typeof(StreamTestQuery), typeof(string), null!);
            var pipelineValidator = new PipelineValidatorBuilder().WithStreamRegistry(registry).Build();

            // Act
            var result = Should.NotThrow(() => pipelineValidator.Validate());

            // Assert — the single error is HandlerRegistered
            result.Errors.Count.ShouldBe(1);
            result.Errors[0].RuleId.ShouldBe(HandlerRegistered);
        }
    }
}
