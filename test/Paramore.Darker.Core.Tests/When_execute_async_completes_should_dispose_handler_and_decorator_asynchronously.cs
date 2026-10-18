using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class SimpleFactoryAsyncDisposalTests
    {
        [Fact]
        public async Task When_execute_async_completes_should_dispose_handler_and_decorator_asynchronously()
        {
            // Arrange — Simple factories, and components that can be disposed either way
            var created = new List<IRecordsDisposal>();
            var handlerFactory = new SimpleHandlerFactory(_ => Track(created, new DisposalRecordingAsyncHandler()));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type => Track(created, (IQueryHandlerDecorator)Activator.CreateInstance(type)));

            var registryAsync = new QueryHandlerRegistryAsync();
            registryAsync.Register<DisposalRecordingQuery, DisposalRecordingResult, DisposalRecordingAsyncHandler>();

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                registryAsync, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new StreamQueryHandlerRegistry());
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            // Act
            var result = await processor.ExecuteAsync(new DisposalRecordingQuery(), cancellationToken: TestContext.Current.CancellationToken);

            // Assert — the query ran, and handler and decorator were each torn down with DisposeAsync
            result.Value.ShouldBe(DisposalRecordingAsyncHandler.RESULT);
            created.Count.ShouldBe(2);
            created.ShouldAllBe(c => c.DisposedBy == "DisposeAsync");
        }

        [Fact]
        public async Task When_execute_stream_completes_should_dispose_handler_and_decorator_asynchronously()
        {
            // Arrange — Simple factories, and components that can be disposed either way
            var created = new List<IRecordsDisposal>();
            var handlerFactory = new SimpleHandlerFactory(_ => Track(created, new DisposalRecordingStreamHandler()));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type => Track(created, (IQueryHandlerDecorator)Activator.CreateInstance(type)));

            var registryStream = new StreamQueryHandlerRegistry();
            registryStream.Register<DisposalRecordingStreamQuery, DisposalRecordingStreamItem, DisposalRecordingStreamHandler>();

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                registryStream);
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());
            var items = new List<DisposalRecordingStreamItem>();

            // Act
            await foreach (var item in processor.ExecuteStream(new DisposalRecordingStreamQuery(), cancellationToken: TestContext.Current.CancellationToken))
                items.Add(item);

            // Assert — the stream ran, and handler and decorator were each torn down with DisposeAsync
            items.Count.ShouldBe(1);
            created.Count.ShouldBe(2);
            created.ShouldAllBe(c => c.DisposedBy == "DisposeAsync");
        }

        private static T Track<T>(List<IRecordsDisposal> created, T component)
        {
            created.Add((IRecordsDisposal)component);
            return component;
        }
    }
}
