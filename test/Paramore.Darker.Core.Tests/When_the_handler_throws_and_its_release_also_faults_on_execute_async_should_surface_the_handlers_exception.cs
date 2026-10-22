using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Core.Tests.Logging;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class AsyncHandlerFaultPrecedenceTests
    {
        private readonly LoggerCaptureFixture _logs;

        public AsyncHandlerFaultPrecedenceTests(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public async Task When_the_handler_throws_and_its_release_also_faults_on_execute_async_should_surface_the_handlers_exception()
        {
            // Arrange
            var handlerFault = new HandlerFaultException("handler failed");
            var releaseFault = new InvalidOperationException("release failed asynchronously");

            var registryAsync = new QueryHandlerRegistryAsync();
            registryAsync.Register<ThrowingExecuteAndDisposeAsyncQuery, ThrowingExecuteAndDisposeAsyncResult,
                ThrowingExecuteAndDisposeAsyncQueryHandler>();

            var handlerFactory = new SimpleHandlerFactory(_ => new ThrowingExecuteAndDisposeAsyncQueryHandler(handlerFault, releaseFault));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                registryAsync, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new StreamQueryHandlerRegistry());
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            // Act
            var thrown = await Should.ThrowAsync<HandlerFaultException>(
                async () => await processor.ExecuteAsync(new ThrowingExecuteAndDisposeAsyncQuery()));

            // Assert — the caller sees the handler's own exception, not the release fault
            thrown.ShouldBeSameAs(handlerFault);

            // Assert — the release fault is logged at Error rather than lost
            ReleaseFaultErrorCount(releaseFault).ShouldBe(1);
        }

        [Fact]
        public async Task When_the_handler_throws_mid_stream_and_its_release_also_faults_on_execute_stream_should_surface_the_handlers_exception()
        {
            // Arrange
            var handlerFault = new HandlerFaultException("stream handler failed");
            var releaseFault = new InvalidOperationException("stream release failed asynchronously");

            var registryStream = new StreamQueryHandlerRegistry();
            registryStream.Register<ThrowingExecuteAndDisposeStreamQuery, ThrowingExecuteAndDisposeStreamItem,
                ThrowingExecuteAndDisposeStreamQueryHandler>();

            var handlerFactory = new SimpleHandlerFactory(_ => new ThrowingExecuteAndDisposeStreamQueryHandler(handlerFault, releaseFault));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                registryStream);
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            // Act
            var thrown = await Should.ThrowAsync<HandlerFaultException>(async () =>
            {
                await foreach (var _ in processor.ExecuteStream(new ThrowingExecuteAndDisposeStreamQuery()))
                {
                }
            });

            // Assert — the caller sees the handler's own exception, not the release fault
            thrown.ShouldBeSameAs(handlerFault);

            // Assert — the release fault is logged at Error rather than lost
            ReleaseFaultErrorCount(releaseFault).ShouldBe(1);
        }

        private int ReleaseFaultErrorCount(Exception releaseFault)
            => _logs.CapturedLogs.Count(e => e.LogLevel == LogLevel.Error && ReferenceEquals(e.Exception, releaseFault));
    }
}
