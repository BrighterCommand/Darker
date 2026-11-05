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
    public class SeveralMisconfigurationsTests
    {
        private static readonly ValidationRuleId HandlerRegistered = new("HandlerRegistered");
        private static readonly ValidationRuleId HandlerMatchesPath = new("HandlerMatchesPath");
        private static readonly ValidationRuleId DecoratorAttributeKind = new("DecoratorAttributeKind");
        private static readonly ValidationRuleId DuplicateStep = new("DuplicateStep");

        [Fact]
        public void When_several_pipelines_are_misconfigured_should_report_every_mistake_in_one_result()
        {
            // Arrange — a null handler, a sync-only handler and a wrong-kind attribute on the async registry,
            // and a duplicate step on the stream registry
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register(typeof(TestQueryB), typeof(int), null!);
            asyncRegistry.Register(typeof(TestQueryA), typeof(Guid), typeof(TestQueryHandler));
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncHandlerWithSyncAttribute>();
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<StreamTestQuery, string, DuplicateStepStreamHandler>();
            var pipelineValidator = new PipelineValidatorBuilder()
                .WithAsyncRegistry(asyncRegistry)
                .WithStreamRegistry(streamRegistry)
                .Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one finding for each rule, across errors and warnings
            result.Errors.ShouldContain(e => e.RuleId == HandlerRegistered);
            result.Errors.ShouldContain(e => e.RuleId == HandlerMatchesPath);
            result.Errors.ShouldContain(e => e.RuleId == DecoratorAttributeKind);
            result.Warnings.ShouldContain(w => w.RuleId == DuplicateStep);
        }
    }
}
