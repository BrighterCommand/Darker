using System;
using Moq;
using Paramore.Darker.Testing.Ports;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class QueryProcessorTests
    {
        private readonly Mock<IQueryHandlerFactory> _handlerFactory;
        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryProcessor _queryProcessor;

        public QueryProcessorTests()
        {
            _handlerRegistry = new QueryHandlerRegistry();
            _handlerFactory = new Mock<IQueryHandlerFactory>();
            var decoratorFactory = new Mock<IQueryHandlerDecoratorFactory>();
            var decoratorRegistry = new Mock<IQueryHandlerDecoratorRegistry>();

            var handlerConfiguration = new HandlerConfiguration(_handlerRegistry, _handlerFactory.Object, decoratorRegistry.Object, decoratorFactory.Object);
            _queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());
        }

        [Fact]
        public void ExecutesQueries()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new TestQueryHandler();

            _handlerRegistry.Register<TestQueryA, Guid, TestQueryHandler>();
            _handlerFactory.Setup(x => x.Create(typeof(TestQueryHandler))).Returns(handler);

            // Act
            var result = _queryProcessor.Execute(new TestQueryA(id));

            // Assert
            result.ShouldBe(id);
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag.ShouldContainKeyAndValue("id", id);
            _handlerFactory.Verify(x => x.Release(handler), Times.Once);
        }

        [Fact]
        public void ExecutesTheMatchingHandler()
        {
            // Arrange
            var id = Guid.NewGuid();

            var handlerA = new Mock<IQueryHandler<TestQueryA, Guid>>();
            var handlerB = new Mock<IQueryHandler<TestQueryB, int>>();

            _handlerRegistry.Register<TestQueryA, Guid, IQueryHandler<TestQueryA, Guid>>();
            _handlerRegistry.Register<TestQueryB, int, IQueryHandler<TestQueryB, int>>();

            _handlerFactory.Setup(x => x.Create(typeof(IQueryHandler<TestQueryA, Guid>))).Returns(handlerA.Object);
            _handlerFactory.Setup(x => x.Create(typeof(IQueryHandler<TestQueryB, int>))).Returns(handlerB.Object);

            // Act
            _queryProcessor.Execute(new TestQueryA(id));

            // Assert
            handlerA.Verify(x => x.Execute(It.Is<TestQueryA>(q => q.Id == id)), Times.Once);
            handlerA.Verify(x => x.Fallback(It.IsAny<TestQueryA>()), Times.Never);
            handlerB.Verify(x => x.Execute(It.IsAny<TestQueryB>()), Times.Never);
            handlerB.Verify(x => x.Fallback(It.IsAny<TestQueryB>()), Times.Never);
            _handlerFactory.Verify(x => x.Release(handlerA.Object), Times.Once);
            _handlerFactory.Verify(x => x.Release(handlerB.Object), Times.Never);
        }

        [Fact]
        public void ExceptionsDontCauseFallbackByDefault()
        {
            // Arrange
            var id = Guid.NewGuid();

            var handlerA = new Mock<IQueryHandler<TestQueryA, Guid>>();
            handlerA.Setup(x => x.Execute(It.Is<TestQueryA>(q => q.Id == id))).Throws<FormatException>();

            _handlerRegistry.Register<TestQueryA, Guid, IQueryHandler<TestQueryA, Guid>>();

            _handlerFactory.Setup(x => x.Create(typeof(IQueryHandler<TestQueryA, Guid>))).Returns(handlerA.Object);

            // Act
            Assert.Throws<FormatException>(() => _queryProcessor.Execute(new TestQueryA(id)));

            // Assert
            handlerA.Verify(x => x.Fallback(It.IsAny<TestQueryA>()), Times.Never);
            _handlerFactory.Verify(x => x.Release(handlerA.Object), Times.Once);
        }
    }
}