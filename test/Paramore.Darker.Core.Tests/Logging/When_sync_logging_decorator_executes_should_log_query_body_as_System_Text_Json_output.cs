using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Logging;
using Paramore.Darker.Logging.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Logging
{
    [Collection("QueryLoggingJsonOptions")]
    public class When_sync_logging_decorator_executes_should_log_query_body_as_System_Text_Json_output
    {
        private readonly LoggerCaptureFixture _logs;

        public When_sync_logging_decorator_executes_should_log_query_body_as_System_Text_Json_output(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public void Execute_should_log_start_with_System_Text_Json_body_and_completion()
        {
            // Arrange — isolate the lock on a throwaway options instance so the shared default
            // is never locked by this test's Serialize (C5); restore the original in finally.
            var original = QueryLoggingJsonOptions.Options;
            _logs.Clear();
            try
            {
                QueryLoggingJsonOptions.Options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    WriteIndented = false
                };

                var query = new CoreLoggingTestQuery { Id = Guid.NewGuid(), Name = "Ada" };

                var registry = new QueryHandlerRegistry();
                registry.Register<CoreLoggingTestQuery, CoreLoggingTestQuery.Result, CoreLoggingTestQueryHandler>();

                var handlerFactory = new SimpleHandlerFactory(_ => new CoreLoggingTestQueryHandler());
                var decoratorFactory = new SimpleHandlerDecoratorFactory(
                    _ => new QueryLoggingDecorator<IQuery<CoreLoggingTestQuery.Result>, CoreLoggingTestQuery.Result>());

                var configuration = new HandlerConfiguration(
                    registry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);

                var queryProcessor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());

                // Act
                queryProcessor.Execute(query);

                // Assert — install-before-touch precondition: the capturing factory was in place
                // when the decorator's static Logger was first initialised (FR10).
                _logs.CapturedLogs.ShouldNotBeEmpty();

                var expectedJson = JsonSerializer.Serialize(query, QueryLoggingJsonOptions.Options);

                var start = _logs.CapturedLogs.Single(e => e.MessageTemplate == "Executing query {QueryName}: {Query}");
                Argument(start, "QueryName").ShouldBe(nameof(CoreLoggingTestQuery));
                Argument(start, "Query").ShouldBe(expectedJson);

                var completion = _logs.CapturedLogs.Single(
                    e => e.MessageTemplate == "Execution of query {QueryName} completed in {Elapsed}ms");
                Argument(completion, "QueryName").ShouldBe(nameof(CoreLoggingTestQuery));
            }
            finally
            {
                QueryLoggingJsonOptions.Options = original;
            }
        }

        private static object Argument(CapturedLogEntry entry, string key)
            => entry.StructuredArguments.Single(kvp => kvp.Key == key).Value;
    }
}
