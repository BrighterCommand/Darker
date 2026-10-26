using System;
using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class DecoratorClosingTests
    {
        private static readonly ValidationRuleId PipelineDescribable = new("PipelineDescribable");

        [Fact]
        public void When_a_decorator_closes_only_over_the_concrete_query_should_report_pipeline_describable()
        {
            // Arrange — a sync handler whose decorator closes over TestQuery but not over IQuery<TestQuery.Result>,
            // which is what the pipeline builder closes it over
            var closingMessage = ClosingMessage(typeof(IQuery<TestQuery.Result>), typeof(TestQuery.Result));
            var registry = new QueryHandlerRegistry();
            registry.Register<TestQuery, TestQuery.Result, QueryConstructingSyncHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — the closing failure the builder would hit is reported
            var finding = result.Errors.Single(e => e.RuleId == PipelineDescribable);
            finding.Message.ShouldContain(nameof(QueryConstructingSyncAttribute));
            finding.Message.ShouldContain(closingMessage);
        }

        [Fact]
        public void When_a_decorator_closes_only_over_the_concrete_query_should_report_pipeline_describable_on_the_async_path()
        {
            // Arrange — an async handler whose decorator closes over TestQuery but not over IQuery<TestQuery.Result>
            var closingMessage = ClosingMessage(typeof(IQuery<TestQuery.Result>), typeof(TestQuery.Result));
            var registry = new QueryHandlerRegistryAsync();
            registry.Register<TestQuery, TestQuery.Result, QueryConstructingAsyncHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — the closing failure the builder would hit is reported
            var finding = result.Errors.Single(e => e.RuleId == PipelineDescribable);
            finding.Message.ShouldContain(nameof(QueryConstructingAsyncAttribute));
            finding.Message.ShouldContain(closingMessage);
        }

        [Fact]
        public void When_a_decorator_closes_only_over_the_concrete_query_should_report_pipeline_describable_on_the_stream_path()
        {
            // Arrange — a stream handler whose decorator closes over StreamTestQuery but not over IStreamQuery<string>
            var closingMessage = ClosingMessage(typeof(IStreamQuery<string>), typeof(string));
            var registry = new StreamQueryHandlerRegistry();
            registry.Register<StreamTestQuery, string, QueryConstructingStreamHandler>();
            var pipelineValidator = new PipelineValidatorBuilder().WithStreamRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — the closing failure the builder would hit is reported
            var finding = result.Errors.Single(e => e.RuleId == PipelineDescribable);
            finding.Message.ShouldContain(nameof(QueryConstructingStreamAttribute));
            finding.Message.ShouldContain(closingMessage);
        }

        private static string ClosingMessage(Type queryType, Type resultType)
        {
            try
            {
                typeof(QueryConstructingDecorator<,>).MakeGenericType(queryType, resultType);
            }
            catch (Exception e)
            {
                return e.Message;
            }

            throw new InvalidOperationException("The decorator type was expected not to close.");
        }
    }
}
