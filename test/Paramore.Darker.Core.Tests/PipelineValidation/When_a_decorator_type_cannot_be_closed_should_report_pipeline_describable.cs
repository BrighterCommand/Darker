using System;
using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class UnclosableDecoratorTypePipelineDescribableRuleTests
    {
        private static readonly ValidationRuleId PipelineDescribable = new("PipelineDescribable");

        [Fact]
        public void When_a_decorator_type_cannot_be_closed_should_report_pipeline_describable_on_the_async_path()
        {
            // Arrange — an async handler whose attribute names a decorator with the wrong generic arity
            var closingMessage = ClosingMessage(typeof(WrongArityDecorator<>), typeof(AsyncTestQuery), typeof(AsyncTestQuery.Result));
            var registry = new QueryHandlerRegistryAsync();
            registry.Register<AsyncTestQuery, AsyncTestQuery.Result, WrongArityAsyncHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            PipelineValidationResult? result = null;
            var exception = Record.Exception(() => result = pipelineValidator.Validate());

            // Assert — no exception; the error names the attribute and carries the MakeGenericType message
            exception.ShouldBeNull();
            var finding = result!.Errors.Single(e => e.RuleId == PipelineDescribable);
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain(nameof(WrongArityAsyncAttribute));
            finding.Message.ShouldContain(closingMessage);
        }

        [Fact]
        public void When_a_decorator_type_cannot_be_closed_should_report_pipeline_describable_on_the_stream_path()
        {
            // Arrange — a stream handler whose attribute names a decorator with the wrong generic arity
            var closingMessage = ClosingMessage(typeof(WrongArityDecorator<>), typeof(StreamTestQuery), typeof(string));
            var registry = new StreamQueryHandlerRegistry();
            registry.Register<StreamTestQuery, string, WrongArityStreamHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithStreamRegistry(registry).Build();

            // Act
            PipelineValidationResult? result = null;
            var exception = Record.Exception(() => result = pipelineValidator.Validate());

            // Assert — no exception; the error names the attribute and carries the MakeGenericType message
            exception.ShouldBeNull();
            var finding = result!.Errors.Single(e => e.RuleId == PipelineDescribable);
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain(nameof(WrongArityStreamAttribute));
            finding.Message.ShouldContain(closingMessage);
        }

        private static string ClosingMessage(Type decoratorType, Type queryType, Type resultType)
        {
            try
            {
                decoratorType.MakeGenericType(queryType, resultType);
            }
            catch (Exception e)
            {
                return e.Message;
            }

            throw new InvalidOperationException("The decorator type was expected not to close.");
        }
    }
}
