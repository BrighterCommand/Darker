using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class QueryProcessorAsyncTests
    {
        private readonly Dictionary<Type, IQueryHandler> _handlers = new Dictionary<Type, IQueryHandler>();
        private readonly RecordingHandlerFactory _handlerFactory;
        private readonly RecordingHandlerFactory _handlerFactoryAsync;
        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryHandlerRegistryAsync _handlerRegistryAsync;
        private readonly IQueryProcessor _queryProcessor;

        public QueryProcessorAsyncTests()
        {
            _handlerRegistry = new QueryHandlerRegistry();
            _handlerRegistryAsync = new QueryHandlerRegistryAsync();

            // Two separate recording factories: the async pipeline CREATES the handler via
            // the async slot and RELEASES it through that same slot, so the async factory records
            // the release and the sync factory never sees it.
            _handlerFactory = new RecordingHandlerFactory(handlerType => _handlers[handlerType]);
            _handlerFactoryAsync = new RecordingHandlerFactory(handlerType => _handlers[handlerType]);

            // A single decorator factory + registry serve both sync and async slots
            // (both interfaces are implemented on one class — ADR 0009; no per-instance assertion).
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new NotImplementedException());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                _handlerRegistry, _handlerFactory, decoratorRegistry, decoratorFactory,
                _handlerRegistryAsync, _handlerFactoryAsync, decoratorRegistry, decoratorFactory);
            _queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());
        }

        [Fact]
        public async Task ExecutesQueries()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new ProcessorQueryHandlerAsync();

            _handlerRegistryAsync.Register<ProcessorQuery, Guid, ProcessorQueryHandlerAsync>();
            _handlers[typeof(ProcessorQueryHandlerAsync)] = handler;

            // Act
            var result = await _queryProcessor.ExecuteAsync(new ProcessorQuery(id), cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            result.ShouldBe(id);
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag.ShouldContainKeyAndValue("id", id);
            // The async pipeline releases through the async factory that created the handler.
            _handlerFactoryAsync.ReleaseCount(handler).ShouldBe(1);
            _handlerFactory.ReleaseCount(handler).ShouldBe(0);
        }

        [Fact]
        public async Task ExecutesTheMatchingHandler()
        {
            // Arrange
            var id = Guid.NewGuid();

            var handlerA = new RecordingQueryHandlerAsync<ProcessorQuery, Guid>(query => query.Id);
            var handlerB = new RecordingQueryHandlerAsync<ProcessorIntQuery, int>(_ => 0);

            _handlerRegistryAsync.Register<ProcessorQuery, Guid, RecordingQueryHandlerAsync<ProcessorQuery, Guid>>();
            _handlerRegistryAsync.Register<ProcessorIntQuery, int, RecordingQueryHandlerAsync<ProcessorIntQuery, int>>();

            _handlers[typeof(RecordingQueryHandlerAsync<ProcessorQuery, Guid>)] = handlerA;
            _handlers[typeof(RecordingQueryHandlerAsync<ProcessorIntQuery, int>)] = handlerB;

            // Act
            var result = await _queryProcessor.ExecuteAsync(new ProcessorQuery(id), cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            result.ShouldBe(id);                            // matching handler ran with the expected query
            handlerA.FallbackCount.ShouldBe(0);
            handlerB.ExecuteCount.ShouldBe(0);              // non-matching handler did not run
            handlerB.FallbackCount.ShouldBe(0);
        }

        [Fact]
        public async Task ExceptionsDontCauseFallbackByDefault()
        {
            // Arrange
            var id = Guid.NewGuid();

            var handlerA = new RecordingQueryHandlerAsync<ProcessorQuery, Guid>(_ => throw new FormatException());

            _handlerRegistryAsync.Register<ProcessorQuery, Guid, RecordingQueryHandlerAsync<ProcessorQuery, Guid>>();
            _handlers[typeof(RecordingQueryHandlerAsync<ProcessorQuery, Guid>)] = handlerA;

            // Act
            await Assert.ThrowsAsync<FormatException>(async () => await _queryProcessor.ExecuteAsync(new ProcessorQuery(id), cancellationToken: TestContext.Current.CancellationToken));

            // Assert
            handlerA.FallbackCount.ShouldBe(0);
        }
    }
}
