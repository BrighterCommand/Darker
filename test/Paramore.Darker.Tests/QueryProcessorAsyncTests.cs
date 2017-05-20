using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class QueryProcessorAsyncTests
    {
        private readonly Mock<IQueryHandlerFactory> _handlerFactory;
        private readonly Mock<IQueryHandlerDecoratorFactory> _decoratorFactory;
        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryProcessor _queryProcessor;

        public QueryProcessorAsyncTests()
        {
            _handlerFactory = new Mock<IQueryHandlerFactory>();
            _decoratorFactory = new Mock<IQueryHandlerDecoratorFactory>();
            _handlerRegistry = new QueryHandlerRegistry();

            var handlerConfiguration = new HandlerConfiguration(_handlerRegistry, _handlerFactory.Object, _decoratorFactory.Object);
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
            var handlerB = new Mock<IQueryHandler<TestQueryB, object>>();

            _handlerRegistry.Register<TestQueryA, Guid, IQueryHandler<TestQueryA, Guid>>();
            _handlerRegistry.Register<TestQueryB, object, IQueryHandler<TestQueryB, object>>();

            _handlerFactory.Setup(x => x.Create(typeof(IQueryHandler<TestQueryA, Guid>))).Returns(handlerA.Object);
            _handlerFactory.Setup(x => x.Create(typeof(IQueryHandler<TestQueryB, object>))).Returns(handlerB.Object);

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

        public class TestQueryA : IQuery<Guid>
        {
            public Guid Id { get; }

            public TestQueryA(Guid id)
            {
                Id = id;
            }
        }

        public class TestQueryB : IQuery<object>
        {
        }

        public class TestQueryHandler : QueryHandlerAsync<TestQueryA, Guid>
        {
            public override Task<Guid> ExecuteAsync(TestQueryA request, CancellationToken cancellationToken = default(CancellationToken))
            {
                Context.Bag.Add("id", request.Id);
                return Task.FromResult(request.Id);
            }
        }
    }
}