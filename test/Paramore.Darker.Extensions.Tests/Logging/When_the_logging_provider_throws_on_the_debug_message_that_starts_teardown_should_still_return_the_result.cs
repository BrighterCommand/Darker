using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Builder;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Logging;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.Logging
{
    [Collection("DarkerHostBootstrap")]
    public class DebugLoggingFaultTeardownTests
    {
        [Fact]
        public void When_the_logging_provider_throws_on_the_debug_message_that_starts_teardown_should_still_return_the_result()
        {
            // Arrange — Debug is enabled, and the logging provider starts throwing at Debug once the handler has run
            var previousFactory = ApplicationLogging.LoggerFactory;
            var provider = new ThrowingLoggerProvider(LogLevel.Debug, armed: false);
            ApplicationLogging.LoggerFactory = DebugThrowingLoggerFactory(provider);
            try
            {
                var registry = new QueryHandlerRegistry();
                registry.Register<DebugLogFaultQuery<DebugLogFaultSyncTag>, DebugLogFaultResult<DebugLogFaultSyncTag>, DebugLogFaultSyncQueryHandler>();
                var queryProcessor = QueryProcessorBuilder.With()
                    .Handlers(registry, _ => new DebugLogFaultSyncQueryHandler(provider), _ => { }, _ => null)
                    .InMemoryQueryContextFactory()
                    .Build();

                // Act — teardown's opening Debug message is written, and the logging call itself throws
                DebugLogFaultResult<DebugLogFaultSyncTag> result = null;
                Should.NotThrow(() => result = queryProcessor.Execute(new DebugLogFaultQuery<DebugLogFaultSyncTag>()));

                // Assert
                result.Value.ShouldBe(DebugLogFaultSyncQueryHandler.RESULT);
            }
            finally
            {
                ApplicationLogging.LoggerFactory = previousFactory;
            }
        }

        [Fact]
        public async Task When_the_logging_provider_throws_on_the_debug_message_that_starts_async_teardown_should_still_return_the_result()
        {
            // Arrange — Debug is enabled, and the logging provider starts throwing at Debug once the handler has run
            var previousFactory = ApplicationLogging.LoggerFactory;
            var provider = new ThrowingLoggerProvider(LogLevel.Debug, armed: false);
            ApplicationLogging.LoggerFactory = DebugThrowingLoggerFactory(provider);
            try
            {
                var registry = new QueryHandlerRegistryAsync();
                registry.Register<DebugLogFaultQuery<DebugLogFaultAsyncTag>, DebugLogFaultResult<DebugLogFaultAsyncTag>, DebugLogFaultAsyncQueryHandler>();
                var handlerFactory = new SimpleHandlerFactory(_ => new DebugLogFaultAsyncQueryHandler(provider));
                var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
                var queryProcessor = new QueryProcessor(
                    new HandlerConfiguration(
                        new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                        registry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                        new StreamQueryHandlerRegistry()),
                    new InMemoryQueryContextFactory());

                // Act — teardown's opening Debug message is written, and the logging call itself throws
                DebugLogFaultResult<DebugLogFaultAsyncTag> result = null;
                await Should.NotThrowAsync(async () =>
                    result = await queryProcessor.ExecuteAsync(new DebugLogFaultQuery<DebugLogFaultAsyncTag>(), cancellationToken: TestContext.Current.CancellationToken));

                // Assert
                result.Value.ShouldBe(DebugLogFaultAsyncQueryHandler.RESULT);
            }
            finally
            {
                ApplicationLogging.LoggerFactory = previousFactory;
            }
        }

        private static ILoggerFactory DebugThrowingLoggerFactory(ThrowingLoggerProvider provider) =>
            new LoggerFactory(new ILoggerProvider[] { provider },
                new LoggerFilterOptions { MinLevel = LogLevel.Debug });
    }
}
