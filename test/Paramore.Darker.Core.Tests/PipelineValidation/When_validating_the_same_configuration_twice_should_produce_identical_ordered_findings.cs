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
    public class DeterministicFindingsTests
    {
        private static readonly ValidationRuleId HandlerRegistered = new("HandlerRegistered");

        [Fact]
        public void When_validating_the_same_configuration_twice_should_produce_identical_ordered_findings_by_registry_then_registration()
        {
            // Arrange — two registrations with no handler in each of the sync, async and stream registries
            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register(typeof(TestQueryA), typeof(Guid), null!);
            syncRegistry.Register(typeof(TestQueryB), typeof(int), null!);
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register(typeof(TestQueryA), typeof(Guid), null!);
            asyncRegistry.Register(typeof(TestQueryB), typeof(int), null!);
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register(typeof(TestQueryA), typeof(Guid), null!);
            streamRegistry.Register(typeof(TestQueryB), typeof(int), null!);
            var pipelineValidator = new PipelineValidatorBuilder()
                .WithSyncRegistry(syncRegistry)
                .WithAsyncRegistry(asyncRegistry)
                .WithStreamRegistry(streamRegistry)
                .Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — sync registry first, then async, then stream; registration order within each
            var messages = result.Errors.Where(e => e.RuleId == HandlerRegistered).Select(e => e.Message).ToList();
            messages.ShouldBe(
            [
                "No handler type is registered for query 'TestQueryA' in the registry path 'Sync'.",
                "No handler type is registered for query 'TestQueryB' in the registry path 'Sync'.",
                "No handler type is registered for query 'TestQueryA' in the registry path 'Async'.",
                "No handler type is registered for query 'TestQueryB' in the registry path 'Async'.",
                "No handler type is registered for query 'TestQueryA' in the registry path 'Stream'.",
                "No handler type is registered for query 'TestQueryB' in the registry path 'Stream'."
            ]);
        }

        [Fact]
        public void When_validating_the_same_configuration_twice_should_produce_identical_ordered_findings_from_separate_validators()
        {
            // Arrange — the same registries behind two validators
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register(typeof(TestQueryB), typeof(int), null!);
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncHandlerWithSyncAttribute>();
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<StreamTestQuery, string, DuplicateStepStreamHandler>();
            var first = new PipelineValidatorBuilder().WithAsyncRegistry(asyncRegistry).WithStreamRegistry(streamRegistry).Build();
            var second = new PipelineValidatorBuilder().WithAsyncRegistry(asyncRegistry).WithStreamRegistry(streamRegistry).Build();

            // Act
            var firstResult = first.Validate();
            var secondResult = second.Validate();

            // Assert
            secondResult.Errors.ShouldBe(firstResult.Errors);
            secondResult.Warnings.ShouldBe(firstResult.Warnings);
            firstResult.Errors.ShouldNotBeEmpty();
            firstResult.Warnings.ShouldNotBeEmpty();
        }
    }
}
