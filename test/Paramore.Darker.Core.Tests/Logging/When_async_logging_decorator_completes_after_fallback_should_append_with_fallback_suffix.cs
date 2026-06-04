using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Logging;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Logging
{
    [Collection("QueryLoggingJsonOptions")]
    public class When_async_logging_decorator_completes_after_fallback_should_append_with_fallback_suffix
    {
        private readonly LoggerCaptureFixture _logs;

        public When_async_logging_decorator_completes_after_fallback_should_append_with_fallback_suffix(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public async Task Completion_template_includes_the_runtime_concatenated_with_fallback_suffix()
        {
            // Arrange — throwaway options so the serialize-lock never touches the shared default (C5)
            var original = QueryLoggingJsonOptions.Options;
            _logs.Clear();
            try
            {
                QueryLoggingJsonOptions.Options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };

                var asyncRegistry = new QueryHandlerRegistryAsync();
                asyncRegistry.Register<CoreLoggingTestQuery, CoreLoggingTestQuery.Result, CoreLoggingFallbackQueryHandlerAsync>();

                var handlerFactory = new SimpleHandlerFactory(_ => new CoreLoggingFallbackQueryHandlerAsync());
                var decoratorFactory = new SimpleHandlerDecoratorFactory(
                    type => (IQueryHandlerDecorator)Activator.CreateInstance(type));

                var configuration = new HandlerConfiguration(
                    new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                    asyncRegistry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);

                var queryProcessor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());

                // Act — the async handler throws, FallbackAsync returns, the bag carries the fallback cause
                await queryProcessor.ExecuteAsync(new CoreLoggingTestQuery { Id = Guid.NewGuid(), Name = "Grace" });

                // Assert the message TEMPLATE ({OriginalFormat}), not the rendered string (FR9)
                _logs.CapturedLogs.ShouldNotBeEmpty();
                _logs.CapturedLogs
                    .Select(e => e.MessageTemplate)
                    .ShouldContain("Async execution of query {QueryName} completed in {Elapsed}ms (with fallback)");
            }
            finally
            {
                QueryLoggingJsonOptions.Options = original;
            }
        }
    }
}
