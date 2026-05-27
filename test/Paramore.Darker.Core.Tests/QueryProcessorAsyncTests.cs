using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Paramore.Darker.Core.Tests.Exported;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class QueryProcessorAsyncTests
    {
        private readonly Mock<IQueryHandlerFactory> _handlerFactory;
        private readonly Mock<IQueryHandlerFactoryAsync> _handlerFactoryAsync;
        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryHandlerRegistryAsync _handlerRegistryAsync;
        private readonly IQueryProcessor _queryProcessor;

        public QueryProcessorAsyncTests()
        {
            _handlerRegistry = new QueryHandlerRegistry();
            _handlerRegistryAsync = new QueryHandlerRegistryAsync();
            _handlerFactory = new Mock<IQueryHandlerFactory>();
            _handlerFactoryAsync = new Mock<IQueryHandlerFactoryAsync>();
            var decoratorFactory = new Mock<IQueryHandlerDecoratorFactory>();
            var decoratorRegistry = new Mock<IQueryHandlerDecoratorRegistry>();
            var decoratorFactoryAsync = new Mock<IQueryHandlerDecoratorFactoryAsync>();
            var decoratorRegistryAsync = new Mock<IQueryHandlerDecoratorRegistryAsync>();

            var handlerConfiguration = new HandlerConfiguration(
                _handlerRegistry, _handlerFactory.Object, decoratorRegistry.Object, decoratorFactory.Object,
                _handlerRegistryAsync, _handlerFactoryAsync.Object, decoratorRegistryAsync.Object, decoratorFactoryAsync.Object);
            _queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());
        }

        [Fact]
        public async Task ExecutesQueries()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new TestQueryHandlerAsync();

            _handlerRegistryAsync.Register<TestQueryA, Guid, TestQueryHandlerAsync>();
            _handlerFactoryAsync.Setup(x => x.Create(typeof(TestQueryHandlerAsync))).Returns(handler);

            // Act
            var result = await _queryProcessor.ExecuteAsync(new TestQueryA(id));

            // Assert
            result.ShouldBe(id);
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag.ShouldContainKeyAndValue("id", id);
            _handlerFactoryAsync.Verify(x => x.Release(handler), Times.Never);
        }

        [Fact]
        public async Task ExecutesTheMatchingHandler()
        {
            // Arrange
            var id = Guid.NewGuid();

            var handlerA = new Mock<IQueryHandlerAsync<TestQueryA, Guid>>();
            var handlerB = new Mock<IQueryHandlerAsync<TestQueryB, int>>();

            _handlerRegistryAsync.Register<TestQueryA, Guid, IQueryHandlerAsync<TestQueryA, Guid>>();
            _handlerRegistryAsync.Register<TestQueryB, int, IQueryHandlerAsync<TestQueryB, int>>();

            _handlerFactoryAsync.Setup(x => x.Create(typeof(IQueryHandlerAsync<TestQueryA, Guid>))).Returns(handlerA.Object);
            _handlerFactoryAsync.Setup(x => x.Create(typeof(IQueryHandlerAsync<TestQueryB, int>))).Returns(handlerB.Object);

            // Act
            await _queryProcessor.ExecuteAsync(new TestQueryA(id));

            // Assert
            handlerA.Verify(x => x.ExecuteAsync(It.Is<TestQueryA>(q => q.Id == id), default(CancellationToken)), Times.Once);
            handlerA.Verify(x => x.FallbackAsync(It.IsAny<TestQueryA>(), default(CancellationToken)), Times.Never);
            handlerB.Verify(x => x.ExecuteAsync(It.IsAny<TestQueryB>(), default(CancellationToken)), Times.Never);
            handlerB.Verify(x => x.FallbackAsync(It.IsAny<TestQueryB>(), default(CancellationToken)), Times.Never);
        }

        [Fact]
        public async Task ExceptionsDontCauseFallbackByDefault()
        {
            // Arrange
            var id = Guid.NewGuid();

            var handlerA = new Mock<IQueryHandlerAsync<TestQueryA, Guid>>();
            handlerA.Setup(x => x.ExecuteAsync(It.Is<TestQueryA>(q => q.Id == id), default(CancellationToken))).Throws<FormatException>();

            _handlerRegistryAsync.Register<TestQueryA, Guid, IQueryHandlerAsync<TestQueryA, Guid>>();
            _handlerFactoryAsync.Setup(x => x.Create(typeof(IQueryHandlerAsync<TestQueryA, Guid>))).Returns(handlerA.Object);

            // Act
            await Assert.ThrowsAsync<FormatException>(async () => await _queryProcessor.ExecuteAsync(new TestQueryA(id)));

            // Assert
            handlerA.Verify(x => x.FallbackAsync(It.IsAny<TestQueryA>(), default(CancellationToken)), Times.Never);
        }
    }
}
