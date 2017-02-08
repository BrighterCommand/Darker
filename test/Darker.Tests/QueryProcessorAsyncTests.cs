using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Xunit;

namespace Darker.Tests
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

            _handlerRegistry.Register<TestQueryA, TestQueryA.Result, TestQueryHandler>();
            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(TestQueryHandler))).Returns(handler);

            // Act
            var response = await _queryProcessor.ExecuteAsync(new TestQueryA(id));

            // Assert
            response.ShouldNotBeNull();
            response.Id.ShouldBe(id);
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag.ShouldContainKeyAndValue("id", id);
        }

        [Fact]
        public async Task ExecutesTheMatchingHandler()
        {
            // Arrange
            var id = Guid.NewGuid();

            var handlerA = new Mock<IQueryHandler<TestQueryA, TestQueryA.Result>>();
            var handlerB = new Mock<IQueryHandler<TestQueryB, TestQueryB.Result>>();

            _handlerRegistry.Register<TestQueryA, TestQueryA.Result, IQueryHandler<TestQueryA, TestQueryA.Result>>();
            _handlerRegistry.Register<TestQueryB, TestQueryB.Result, IQueryHandler<TestQueryB, TestQueryB.Result>>();

            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(IQueryHandler<TestQueryA, TestQueryA.Result>))).Returns(handlerA.Object);
            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(IQueryHandler<TestQueryB, TestQueryB.Result>))).Returns(handlerB.Object);

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

            var handlerA = new Mock<IQueryHandler<TestQueryA, TestQueryA.Result>>();
            handlerA.Setup(x => x.ExecuteAsync(It.Is<TestQueryA>(q => q.Id == id), default(CancellationToken))).Throws<FormatException>();

            _handlerRegistry.Register<TestQueryA, TestQueryA.Result, IQueryHandler<TestQueryA, TestQueryA.Result>>();

            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(IQueryHandler<TestQueryA, TestQueryA.Result>))).Returns(handlerA.Object);

            // Act
            await Assert.ThrowsAsync<FormatException>(async () => await _queryProcessor.ExecuteAsync(new TestQueryA(id)));

            // Assert
            handlerA.Verify(x => x.FallbackAsync(It.IsAny<TestQueryA>(), default(CancellationToken)), Times.Never);
        }

        public class TestQueryA : IQuery<TestQueryA.Result>
        {
            public Guid Id { get; }

            public TestQueryA(Guid id)
            {
                Id = id;
            }

            public class Result
            {
                public Guid Id { get; }

                public Result(Guid id)
                {
                    Id = id;
                }
            }
        }

        public class TestQueryB : IQuery<TestQueryB.Result>
        {
            public class Result { }
        }

        public class TestQueryHandler : AsyncQueryHandler<TestQueryA, TestQueryA.Result>
        {
            public override Task<TestQueryA.Result> ExecuteAsync(TestQueryA request, CancellationToken cancellationToken = default(CancellationToken))
            {
                Context.Bag.Add("id", request.Id);
                return Task.FromResult(new TestQueryA.Result(request.Id));
            }
        }
    }
}