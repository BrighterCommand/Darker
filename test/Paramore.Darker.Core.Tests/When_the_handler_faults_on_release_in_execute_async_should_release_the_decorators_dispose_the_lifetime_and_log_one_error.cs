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
    public class AsyncTeardownReleaseFaultTests
    {
        private readonly LoggerCaptureFixture _logs;

        public AsyncTeardownReleaseFaultTests(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public async Task When_the_handler_faults_on_release_in_execute_async_should_release_the_decorators_dispose_the_lifetime_and_log_one_error()
        {
            // Arrange — only the handler's DisposeAsync faults
            var handlerFault = new InvalidOperationException("handler release failed asynchronously");
            var lifetimeItem = new RecordingScope();
            var created = new List<IRecordsAsyncDisposal>();
            var processor = AsyncProcessor<HandlerFaultsTag>(handlerFault, new Exception[] { null, null }, lifetimeItem, created);

            // Act
            var result = await processor.ExecuteAsync(new AsyncReleaseFaultQuery<HandlerFaultsTag>(), cancellationToken: TestContext.Current.CancellationToken);

            // Assert — the result returns, every component was released, and the lifetime was disposed
            result.Value.ShouldBe(AsyncReleaseFaultHandler<HandlerFaultsTag>.RESULT);
            created.Count.ShouldBe(3);
            created.ShouldAllBe(c => c.DisposeAsyncCount == 1);
            lifetimeItem.DisposeAsyncCount.ShouldBe(1);

            // Assert — one Error, naming the handler type
            var errors = ErrorsFor(handlerFault);
            errors.Count.ShouldBe(1);
            errors[0].RenderedMessage.ShouldContain(nameof(AsyncReleaseFaultHandler<HandlerFaultsTag>));
        }

        [Fact]
        public async Task When_every_component_faults_on_release_in_execute_async_should_log_one_error_per_component_and_dispose_the_lifetime()
        {
            // Arrange — the handler and both decorators fault in DisposeAsync
            var handlerFault = new InvalidOperationException("handler release failed asynchronously");
            var firstDecoratorFault = new InvalidOperationException("first decorator release failed asynchronously");
            var secondDecoratorFault = new InvalidOperationException("second decorator release failed asynchronously");
            var lifetimeItem = new RecordingScope();
            var created = new List<IRecordsAsyncDisposal>();
            var processor = AsyncProcessor<EveryComponentFaultsTag>(
                handlerFault, new Exception[] { firstDecoratorFault, secondDecoratorFault }, lifetimeItem, created);

            // Act
            var result = await processor.ExecuteAsync(new AsyncReleaseFaultQuery<EveryComponentFaultsTag>(), cancellationToken: TestContext.Current.CancellationToken);

            // Assert — the result returns, every component was released, and the lifetime was disposed
            result.Value.ShouldBe(AsyncReleaseFaultHandler<EveryComponentFaultsTag>.RESULT);
            created.Count.ShouldBe(3);
            created.ShouldAllBe(c => c.DisposeAsyncCount == 1);
            lifetimeItem.DisposeAsyncCount.ShouldBe(1);

            // Assert — one Error per fault, each naming its component's type
            var faults = new[] { handlerFault, firstDecoratorFault, secondDecoratorFault };
            var errors = _logs.CapturedLogs
                .Where(e => e.LogLevel == LogLevel.Error && faults.Any(f => ReferenceEquals(e.Exception, f)))
                .ToList();
            errors.Count.ShouldBe(3);
            errors.Single(e => ReferenceEquals(e.Exception, handlerFault)).RenderedMessage
                .ShouldContain(nameof(AsyncReleaseFaultHandler<EveryComponentFaultsTag>));
            errors.Single(e => ReferenceEquals(e.Exception, firstDecoratorFault)).RenderedMessage
                .ShouldContain(nameof(FaultOnDisposeAsyncDecorator<IQuery<int>, int>));
            errors.Single(e => ReferenceEquals(e.Exception, secondDecoratorFault)).RenderedMessage
                .ShouldContain(nameof(FaultOnDisposeAsyncDecorator<IQuery<int>, int>));
        }

        [Fact]
        public async Task When_the_handler_faults_on_release_in_execute_stream_should_yield_every_item_release_the_decorators_and_dispose_the_lifetime()
        {
            // Arrange — only the handler's DisposeAsync faults
            var handlerFault = new InvalidOperationException("stream handler release failed asynchronously");
            var lifetimeItem = new RecordingScope();
            var created = new List<IRecordsAsyncDisposal>();

            Func<Type, IQueryHandler> createHandler = _ => Track(created, new StreamReleaseFaultHandler<StreamHandlerFaultsTag>(handlerFault));
            var handlerFactory = new LifetimeAttachingHandlerFactory(createHandler, lifetimeItem);
            var decoratorFactory = DecoratorFactory(new Exception[] { null, null }, created);

            var registryStream = new StreamQueryHandlerRegistry();
            registryStream.Register<StreamReleaseFaultQuery<StreamHandlerFaultsTag>, StreamReleaseFaultItem<StreamHandlerFaultsTag>,
                StreamReleaseFaultHandler<StreamHandlerFaultsTag>>();

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                registryStream);
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());
            var items = new List<StreamReleaseFaultItem<StreamHandlerFaultsTag>>();

            // Act
            await foreach (var item in processor.ExecuteStream(new StreamReleaseFaultQuery<StreamHandlerFaultsTag>(), cancellationToken: TestContext.Current.CancellationToken))
                items.Add(item);

            // Assert — every item was yielded, every component was released, and the lifetime was disposed
            items.Count.ShouldBe(2);
            created.Count.ShouldBe(3);
            created.ShouldAllBe(c => c.DisposeAsyncCount == 1);
            lifetimeItem.DisposeAsyncCount.ShouldBe(1);

            // Assert — one Error, naming the handler type
            var errors = ErrorsFor(handlerFault);
            errors.Count.ShouldBe(1);
            errors[0].RenderedMessage.ShouldContain(nameof(StreamReleaseFaultHandler<StreamHandlerFaultsTag>));
        }

        private static QueryProcessor AsyncProcessor<TTag>(
            Exception handlerFault, Exception[] decoratorFaults, RecordingScope lifetimeItem, List<IRecordsAsyncDisposal> created)
        {
            Func<Type, IQueryHandler> createHandler = _ => Track(created, new AsyncReleaseFaultHandler<TTag>(handlerFault));
            var handlerFactory = new LifetimeAttachingHandlerFactory(createHandler, lifetimeItem);
            var decoratorFactory = DecoratorFactory(decoratorFaults, created);

            var registryAsync = new QueryHandlerRegistryAsync();
            registryAsync.Register<AsyncReleaseFaultQuery<TTag>, AsyncReleaseFaultResult<TTag>, AsyncReleaseFaultHandler<TTag>>();

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                registryAsync, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new StreamQueryHandlerRegistry());
            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }

        // Hands each decorator the next fault in order of creation (null means it releases cleanly).
        private static SimpleHandlerDecoratorFactory DecoratorFactory(Exception[] faults, List<IRecordsAsyncDisposal> created)
        {
            var next = 0;
            return new SimpleHandlerDecoratorFactory(type =>
                Track(created, (IQueryHandlerDecorator)Activator.CreateInstance(type, new object[] { faults[next++] })));
        }

        private static T Track<T>(List<IRecordsAsyncDisposal> created, T component)
        {
            created.Add((IRecordsAsyncDisposal)component);
            return component;
        }

        private List<CapturedLogEntry> ErrorsFor(Exception fault)
            => _logs.CapturedLogs
                .Where(e => e.LogLevel == LogLevel.Error && ReferenceEquals(e.Exception, fault))
                .ToList();
    }
}
