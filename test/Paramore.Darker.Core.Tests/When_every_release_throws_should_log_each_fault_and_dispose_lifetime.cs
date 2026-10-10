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
    public class When_every_release_throws_should_log_each_fault_and_dispose_lifetime
    {
        private readonly LoggerCaptureFixture _logs;

        public When_every_release_throws_should_log_each_fault_and_dispose_lifetime(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public void When_the_handler_and_both_decorators_throw_on_release_should_log_one_error_per_fault_dispose_lifetime_and_return_result()
        {
            // Arrange — the handler and each of two decorators throw a distinct fault from Dispose
            var handlerFault = new InvalidOperationException("handler release failed");
            var firstDecoratorFault = new InvalidOperationException("first decorator release failed");
            var secondDecoratorFault = new InvalidOperationException("second decorator release failed");
            var recorder = new RecordingDisposable("lifetime-item", new List<string>());

            var registry = new QueryHandlerRegistry();
            registry.Register<EveryReleaseThrowsQuery, EveryReleaseThrowsResult, EveryReleaseThrowsQueryHandler>();

            var handlerFactory = new LifetimeAttachingHandlerFactory(_ => new EveryReleaseThrowsQueryHandler(handlerFault), recorder);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(decoratorType =>
                decoratorType.GetGenericTypeDefinition() == typeof(FirstThrowOnDisposeDecorator<,>)
                    ? (IQueryHandlerDecorator)Activator.CreateInstance(decoratorType, firstDecoratorFault)
                    : (IQueryHandlerDecorator)Activator.CreateInstance(decoratorType, secondDecoratorFault));

            var config = new HandlerConfiguration(registry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            // Act
            var result = processor.Execute(new EveryReleaseThrowsQuery());

            // Assert — the query still returns its result and the lifetime is still disposed
            result.Value.ShouldBe(EveryReleaseThrowsQueryHandler.RESULT);
            recorder.DisposeCount.ShouldBe(1);

            // Assert — each fault is logged exactly once at Error, naming its component's type
            var faults = new[] { handlerFault, firstDecoratorFault, secondDecoratorFault };
            var errors = _logs.CapturedLogs
                .Where(e => e.LogLevel == LogLevel.Error && faults.Any(f => ReferenceEquals(e.Exception, f)))
                .ToList();
            errors.Count.ShouldBe(3);

            ErrorFor(errors, handlerFault).ShouldContain(nameof(EveryReleaseThrowsQueryHandler));
            ErrorFor(errors, firstDecoratorFault).ShouldContain(nameof(FirstThrowOnDisposeDecorator<IQuery<EveryReleaseThrowsResult>, EveryReleaseThrowsResult>));
            ErrorFor(errors, secondDecoratorFault).ShouldContain(nameof(SecondThrowOnDisposeDecorator<IQuery<EveryReleaseThrowsResult>, EveryReleaseThrowsResult>));
        }

        private static string ErrorFor(IEnumerable<CapturedLogEntry> errors, Exception fault)
            => errors.Single(e => ReferenceEquals(e.Exception, fault)).RenderedMessage;
    }
}
