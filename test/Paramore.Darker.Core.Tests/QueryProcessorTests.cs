using System;
using System.Collections.Generic;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class QueryProcessorTests
    {
        private readonly Dictionary<Type, IQueryHandler> _handlers = new Dictionary<Type, IQueryHandler>();
        private readonly RecordingHandlerFactory _handlerFactory;
        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryProcessor _queryProcessor;

        public QueryProcessorTests()
        {
            _handlerRegistry = new QueryHandlerRegistry();
            _handlerFactory = new RecordingHandlerFactory(handlerType => _handlers[handlerType]);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new NotImplementedException());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(_handlerRegistry, _handlerFactory, decoratorRegistry, decoratorFactory);
            _queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());
        }

        [Fact]
        public void ExecutesQueries()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new ProcessorQueryHandler();

            _handlerRegistry.Register<ProcessorQuery, Guid, ProcessorQueryHandler>();
            _handlers[typeof(ProcessorQueryHandler)] = handler;

            // Act
            var result = _queryProcessor.Execute(new ProcessorQuery(id));

            // Assert
            result.ShouldBe(id);
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag.ShouldContainKeyAndValue("id", id);
            _handlerFactory.ReleaseCount(handler).ShouldBe(1);
        }

        [Fact]
        public void ExecutesTheMatchingHandler()
        {
            // Arrange
            var id = Guid.NewGuid();

            var handlerA = new RecordingQueryHandler<ProcessorQuery, Guid>(query => query.Id);
            var handlerB = new RecordingQueryHandler<ProcessorIntQuery, int>(_ => 0);

            _handlerRegistry.Register<ProcessorQuery, Guid, RecordingQueryHandler<ProcessorQuery, Guid>>();
            _handlerRegistry.Register<ProcessorIntQuery, int, RecordingQueryHandler<ProcessorIntQuery, int>>();

            _handlers[typeof(RecordingQueryHandler<ProcessorQuery, Guid>)] = handlerA;
            _handlers[typeof(RecordingQueryHandler<ProcessorIntQuery, int>)] = handlerB;

            // Act
            var result = _queryProcessor.Execute(new ProcessorQuery(id));

            // Assert
            result.ShouldBe(id);                            // matching handler ran with the expected query
            handlerA.FallbackCount.ShouldBe(0);
            handlerB.ExecuteCount.ShouldBe(0);              // non-matching handler did not run
            handlerB.FallbackCount.ShouldBe(0);
            _handlerFactory.ReleaseCount(handlerA).ShouldBe(1);
            _handlerFactory.ReleaseCount(handlerB).ShouldBe(0);
        }

        [Fact]
        public void ExceptionsDontCauseFallbackByDefault()
        {
            // Arrange
            var id = Guid.NewGuid();

            var handlerA = new RecordingQueryHandler<ProcessorQuery, Guid>(_ => throw new FormatException());

            _handlerRegistry.Register<ProcessorQuery, Guid, RecordingQueryHandler<ProcessorQuery, Guid>>();
            _handlers[typeof(RecordingQueryHandler<ProcessorQuery, Guid>)] = handlerA;

            // Act
            Assert.Throws<FormatException>(() => _queryProcessor.Execute(new ProcessorQuery(id)));

            // Assert
            handlerA.FallbackCount.ShouldBe(0);
            _handlerFactory.ReleaseCount(handlerA).ShouldBe(1);
        }
    }
}
