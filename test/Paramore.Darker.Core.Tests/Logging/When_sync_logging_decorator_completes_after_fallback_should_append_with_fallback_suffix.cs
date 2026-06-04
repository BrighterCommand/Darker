using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Logging;
using Paramore.Darker.Policies.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Logging
{
    [Collection("QueryLoggingJsonOptions")]
    public class When_sync_logging_decorator_completes_after_fallback_should_append_with_fallback_suffix
    {
        private readonly LoggerCaptureFixture _logs;

        public When_sync_logging_decorator_completes_after_fallback_should_append_with_fallback_suffix(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public void Completion_template_includes_the_runtime_concatenated_with_fallback_suffix()
        {
            // Arrange — throwaway options so the lock never touches the shared default (C5)
            var original = QueryLoggingJsonOptions.Options;
            _logs.Clear();
            try
            {
                QueryLoggingJsonOptions.Options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };

                var registry = new QueryHandlerRegistry();
                registry.Register<CoreLoggingTestQuery, CoreLoggingTestQuery.Result, CoreLoggingFallbackQueryHandler>();

                var handlerFactory = new SimpleHandlerFactory(_ => new CoreLoggingFallbackQueryHandler());
                // [QueryLogging(1)] (outer) wraps [FallbackPolicy(2)] (inner); the factory builds whichever
                // closed decorator the pipeline asks for.
                var decoratorFactory = new SimpleHandlerDecoratorFactory(
                    type => (IQueryHandlerDecorator)Activator.CreateInstance(type));

                var configuration = new HandlerConfiguration(
                    registry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);

                var queryProcessor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());

                // Act — the handler throws, the fallback returns, the bag carries the fallback cause
                queryProcessor.Execute(new CoreLoggingTestQuery { Id = Guid.NewGuid(), Name = "Grace" });

                // Assert — assert the message TEMPLATE ({OriginalFormat}), not the rendered string, so a
                // future refactor to a structured {Fallback} placeholder is caught (FR9).
                _logs.CapturedLogs.ShouldNotBeEmpty();
                _logs.CapturedLogs
                    .Select(e => e.MessageTemplate)
                    .ShouldContain("Execution of query {QueryName} completed in {Elapsed}ms (with fallback)");
            }
            finally
            {
                QueryLoggingJsonOptions.Options = original;
            }
        }
    }
}
