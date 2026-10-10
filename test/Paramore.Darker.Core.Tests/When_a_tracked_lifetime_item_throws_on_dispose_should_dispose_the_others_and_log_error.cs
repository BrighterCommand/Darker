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
    public class When_a_tracked_lifetime_item_throws_on_dispose_should_dispose_the_others_and_log_error
    {
        private readonly LoggerCaptureFixture _logs;

        public When_a_tracked_lifetime_item_throws_on_dispose_should_dispose_the_others_and_log_error(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public void When_a_tracked_item_throws_on_dispose_should_dispose_the_others_newest_first_and_log_at_error()
        {
            // Arrange
            var disposalLog = new List<string>();
            var fault = new InvalidOperationException("middle item failed to dispose");
            var oldest = new RecordingDisposable("oldest", disposalLog);
            var throwing = new ThrowOnDisposeDisposable("middle", disposalLog, fault);
            var newest = new RecordingDisposable("newest", disposalLog);

            var registry = new QueryHandlerRegistry();
            registry.Register<LifetimeIsolationQuery, LifetimeIsolationResult, LifetimeIsolationQueryHandler>();

            var handlerFactory = new LifetimeAttachingHandlerFactory(
                _ => new LifetimeIsolationQueryHandler(), oldest, throwing, newest);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);

            var config = new HandlerConfiguration(
                registry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new StreamQueryHandlerRegistry());

            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            // Act
            var result = processor.Execute(new LifetimeIsolationQuery());

            // Assert — the query still returns its result
            result.Value.ShouldBe(LifetimeIsolationQueryHandler.RESULT);

            // Assert — the non-throwing items were disposed, newest first, despite the middle one throwing
            newest.DisposeCount.ShouldBe(1);
            oldest.DisposeCount.ShouldBe(1);
            disposalLog.ShouldBe(new[] { "newest", "middle", "oldest" });

            // Assert — the fault is logged once at Error, carrying the exception and naming the item's type
            var errors = _logs.CapturedLogs
                .Where(e => e.LogLevel == LogLevel.Error && ReferenceEquals(e.Exception, fault))
                .ToList();
            errors.Count.ShouldBe(1);
            errors[0].RenderedMessage.ShouldContain(nameof(ThrowOnDisposeDisposable));
        }
    }
}
