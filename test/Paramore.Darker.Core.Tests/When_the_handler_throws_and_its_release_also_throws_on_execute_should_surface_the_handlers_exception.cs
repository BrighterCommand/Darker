using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Core.Tests.Logging;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class SyncHandlerFaultPrecedenceTests
    {
        private readonly LoggerCaptureFixture _logs;

        public SyncHandlerFaultPrecedenceTests(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public void When_the_handler_throws_and_its_release_also_throws_on_execute_should_surface_the_handlers_exception()
        {
            // Arrange
            var handlerFault = new HandlerFaultException("handler failed");
            var releaseFault = new InvalidOperationException("release failed");

            var registry = new QueryHandlerRegistry();
            registry.Register<ThrowingExecuteAndDisposeQuery, ThrowingExecuteAndDisposeResult, ThrowingExecuteAndDisposeQueryHandler>();

            var handlerFactory = new SimpleHandlerFactory(_ => new ThrowingExecuteAndDisposeQueryHandler(handlerFault, releaseFault));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);

            var config = new HandlerConfiguration(registry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            // Act
            var thrown = Should.Throw<HandlerFaultException>(() => processor.Execute(new ThrowingExecuteAndDisposeQuery()));

            // Assert — the caller sees the handler's own exception, not the release fault
            thrown.ShouldBeSameAs(handlerFault);

            // Assert — the release fault is logged at Error rather than lost
            _logs.CapturedLogs
                .Count(e => e.LogLevel == LogLevel.Error && ReferenceEquals(e.Exception, releaseFault))
                .ShouldBe(1);
        }
    }
}
