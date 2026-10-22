using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Core.Tests.Logging;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class HandlerDisposeFaultTeardownTests
    {
        private readonly LoggerCaptureFixture _logs;

        public HandlerDisposeFaultTeardownTests(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public void When_the_handler_dispose_throws_on_execute_should_still_release_decorators_dispose_lifetime_and_log_at_error()
        {
            // Arrange
            var fault = new InvalidOperationException("handler failed to dispose");
            var recorder = new RecordingScope();
            var created = new List<IQueryHandlerDecorator>();

            var registry = new QueryHandlerRegistry();
            registry.Register<ThrowingDisposeQuery, ThrowingDisposeResult, ThrowingDisposeQueryHandler>();

            var handlerFactory = new LifetimeAttachingHandlerFactory(_ => new ThrowingDisposeQueryHandler(fault), recorder);
            var decoratorFactory = new RecordingDecoratorFactory(decoratorType =>
            {
                var decorator = (IQueryHandlerDecorator)Activator.CreateInstance(decoratorType);
                created.Add(decorator);
                return decorator;
            });

            var config = new HandlerConfiguration(registry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            // Act
            var result = processor.Execute(new ThrowingDisposeQuery());

            // Assert — the query still returns its result
            result.Value.ShouldBe(ThrowingDisposeQueryHandler.RESULT);

            // Assert — both decorators were still released
            created.Count.ShouldBe(2);
            decoratorFactory.Released.Count.ShouldBe(2);
            created.ShouldAllBe(d => decoratorFactory.ReleaseCount(d) == 1);

            // Assert — the lifetime was still disposed
            recorder.DisposeCount.ShouldBe(1);

            // Assert — the fault is logged once at Error, carrying the exception and naming the handler type
            var errors = _logs.CapturedLogs
                .Where(e => e.LogLevel == LogLevel.Error && ReferenceEquals(e.Exception, fault))
                .ToList();
            errors.Count.ShouldBe(1);
            errors[0].RenderedMessage.ShouldContain(nameof(ThrowingDisposeQueryHandler));
        }
    }
}
