using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Logging;
using Paramore.Darker.Logging.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Logging
{
    [Collection("QueryLoggingJsonOptions")]
    public class When_async_logging_decorator_executes_should_log_query_body_as_System_Text_Json_output
    {
        private readonly LoggerCaptureFixture _logs;

        public When_async_logging_decorator_executes_should_log_query_body_as_System_Text_Json_output(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public async Task ExecuteAsync_should_log_start_with_System_Text_Json_body_and_completion()
        {
            // Arrange — throwaway options so the serialize-lock never touches the shared default (C5)
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

                var asyncRegistry = new QueryHandlerRegistryAsync();
                asyncRegistry.Register<CoreLoggingTestQuery, CoreLoggingTestQuery.Result, CoreLoggingTestQueryHandlerAsync>();

                var handlerFactory = new SimpleHandlerFactory(_ => new CoreLoggingTestQueryHandlerAsync());
                var decoratorFactory = new SimpleHandlerDecoratorFactory(
                    _ => new QueryLoggingDecoratorAsync<IQuery<CoreLoggingTestQuery.Result>, CoreLoggingTestQuery.Result>());

                var configuration = new HandlerConfiguration(
                    new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                    asyncRegistry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);

                var queryProcessor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());

                // Act
                await queryProcessor.ExecuteAsync(query);

                // Assert — install-before-touch precondition (FR10), then async templates
                _logs.CapturedLogs.ShouldNotBeEmpty();

                var expectedJson = JsonSerializer.Serialize(query, QueryLoggingJsonOptions.Options);

                var start = _logs.CapturedLogs.Single(e => e.MessageTemplate == "Executing async query {QueryName}: {Query}");
                Argument(start, "QueryName").ShouldBe(nameof(CoreLoggingTestQuery));
                Argument(start, "Query").ShouldBe(expectedJson);

                var completion = _logs.CapturedLogs.Single(
                    e => e.MessageTemplate == "Async execution of query {QueryName} completed in {Elapsed}ms");
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
