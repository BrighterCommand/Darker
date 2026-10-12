using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Core.Tests.Logging;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_tracked_item_dispose_async_faults_should_dispose_the_others_and_log_error
    {
        private readonly LoggerCaptureFixture _logs;

        public When_tracked_item_dispose_async_faults_should_dispose_the_others_and_log_error(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public async Task When_a_tracked_item_faults_in_dispose_async_on_execute_async_should_dispose_the_others_newest_first_and_log_at_error()
        {
            // Arrange
            var disposalLog = new List<string>();
            var fault = new InvalidOperationException("middle item failed to dispose asynchronously");
            var oldest = new RecordingAsyncDisposable("oldest", disposalLog);
            var faulting = new ThrowOnDisposeAsyncDisposable("middle", disposalLog, fault);
            var newest = new RecordingAsyncDisposable("newest", disposalLog);

            var registryAsync = new QueryHandlerRegistryAsync();
            registryAsync.Register<AsyncLifetimeFaultQuery, AsyncLifetimeFaultResult, AsyncLifetimeFaultQueryHandler>();

            var handlerFactory = new LifetimeAttachingHandlerFactory(
                _ => new AsyncLifetimeFaultQueryHandler(), oldest, faulting, newest);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                registryAsync, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new StreamQueryHandlerRegistry());

            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            // Act
            var result = await processor.ExecuteAsync(new AsyncLifetimeFaultQuery());

            // Assert — the query still returns its result
            result.Value.ShouldBe(AsyncLifetimeFaultQueryHandler.RESULT);

            // Assert — the other items were disposed asynchronously, newest first, despite the middle one faulting
            newest.DisposeAsyncCount.ShouldBe(1);
            oldest.DisposeAsyncCount.ShouldBe(1);
            disposalLog.ShouldBe(new[] { "newest", "middle", "oldest" });

            // Assert — the fault is logged once at Error, carrying the exception and naming the item's type
            var errors = _logs.CapturedLogs
                .Where(e => e.LogLevel == LogLevel.Error && ReferenceEquals(e.Exception, fault))
                .ToList();
            errors.Count.ShouldBe(1);
            errors[0].RenderedMessage.ShouldContain(nameof(ThrowOnDisposeAsyncDisposable));
        }

        [Fact]
        public async Task When_a_tracked_item_faults_in_dispose_async_on_execute_stream_should_dispose_the_others_newest_first_and_log_at_error()
        {
            // Arrange
            var disposalLog = new List<string>();
            var fault = new InvalidOperationException("middle item failed to dispose asynchronously on a stream");
            var oldest = new RecordingAsyncDisposable("oldest", disposalLog);
            var faulting = new ThrowOnDisposeAsyncDisposable("middle", disposalLog, fault);
            var newest = new RecordingAsyncDisposable("newest", disposalLog);

            var registryStream = new StreamQueryHandlerRegistry();
            registryStream.Register<StreamLifetimeFaultQuery, StreamLifetimeFaultItem, StreamLifetimeFaultQueryHandler>();

            var handlerFactory = new LifetimeAttachingHandlerFactory(
                _ => new StreamLifetimeFaultQueryHandler(), oldest, faulting, newest);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                registryStream);

            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());
            var items = new List<StreamLifetimeFaultItem>();

            // Act
            await foreach (var item in processor.ExecuteStream(new StreamLifetimeFaultQuery()))
                items.Add(item);

            // Assert — the items are still yielded
            items.Single().Value.ShouldBe(StreamLifetimeFaultQueryHandler.ITEM);

            // Assert — the other items were disposed asynchronously, newest first, despite the middle one faulting
            newest.DisposeAsyncCount.ShouldBe(1);
            oldest.DisposeAsyncCount.ShouldBe(1);
            disposalLog.ShouldBe(new[] { "newest", "middle", "oldest" });

            // Assert — the fault is logged once at Error, carrying the exception and naming the item's type
            var errors = _logs.CapturedLogs
                .Where(e => e.LogLevel == LogLevel.Error && ReferenceEquals(e.Exception, fault))
                .ToList();
            errors.Count.ShouldBe(1);
            errors[0].RenderedMessage.ShouldContain(nameof(ThrowOnDisposeAsyncDisposable));
        }
    }
}
