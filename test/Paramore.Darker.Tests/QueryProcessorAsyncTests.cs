using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Paramore.Darker.Testing.Ports;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class QueryProcessorAsyncTests
    {
        private readonly Mock<IQueryHandlerFactory> _handlerFactory;
        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryProcessor _queryProcessor;

        public QueryProcessorAsyncTests()
        {
            _handlerRegistry = new QueryHandlerRegistry();
            _handlerFactory = new Mock<IQueryHandlerFactory>();
            var decoratorFactory = new Mock<IQueryHandlerDecoratorFactory>();
            var decoratorRegistry = new Mock<IQueryHandlerDecoratorRegistry>();

            var handlerConfiguration = new HandlerConfiguration(_handlerRegistry, _handlerFactory.Object, decoratorRegistry.Object, decoratorFactory.Object);
            _queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());
        }

        [Fact]
        public async Task ExecutesQueries()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new TestQueryHandler();

            _handlerRegistry.Register<TestQueryA, Guid, TestQueryHandler>();
            _handlerFactory.Setup(x => x.Create(typeof(TestQueryHandler))).Returns(handler);

            // Act
            var result = await _queryProcessor.ExecuteAsync(new TestQueryA(id));

            // Assert
            result.ShouldBe(id);
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag.ShouldContainKeyAndValue("id", id);
            _handlerFactory.Verify(x => x.Release(handler), Times.Once);
        }

        [Fact]
        public async Task ExecutesTheMatchingHandler()
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
            await _queryProcessor.ExecuteAsync(new TestQueryA(id));

            // Assert
            handlerA.Verify(x => x.ExecuteAsync(It.Is<TestQueryA>(q => q.Id == id), default(CancellationToken)), Times.Once);
            handlerA.Verify(x => x.FallbackAsync(It.IsAny<TestQueryA>(), default(CancellationToken)), Times.Never);
            handlerB.Verify(x => x.ExecuteAsync(It.IsAny<TestQueryB>(), default(CancellationToken)), Times.Never);
            handlerB.Verify(x => x.FallbackAsync(It.IsAny<TestQueryB>(), default(CancellationToken)), Times.Never);
            _handlerFactory.Verify(x => x.Release(handlerA.Object), Times.Once);
            _handlerFactory.Verify(x => x.Release(handlerB.Object), Times.Never);
        }

        [Fact]
        public async Task ExceptionsDontCauseFallbackByDefault()
        {
            // Arrange
            var id = Guid.NewGuid();

            var handlerA = new Mock<IQueryHandler<TestQueryA, Guid>>();
            handlerA.Setup(x => x.ExecuteAsync(It.Is<TestQueryA>(q => q.Id == id), default(CancellationToken))).Throws<FormatException>();

            _handlerRegistry.Register<TestQueryA, Guid, IQueryHandler<TestQueryA, Guid>>();
            _handlerFactory.Setup(x => x.Create(typeof(IQueryHandler<TestQueryA, Guid>))).Returns(handlerA.Object);

            // Act
            await Assert.ThrowsAsync<FormatException>(async () => await _queryProcessor.ExecuteAsync(new TestQueryA(id)));

            // Assert
            handlerA.Verify(x => x.FallbackAsync(It.IsAny<TestQueryA>(), default(CancellationToken)), Times.Never);
            _handlerFactory.Verify(x => x.Release(handlerA.Object), Times.Once);
        }
    }
}