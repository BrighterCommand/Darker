using System;
using System.Threading;
using System.Threading.Tasks;
using Darker.Serialization;
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
            _queryProcessor = new QueryProcessor(handlerConfiguration, new PolicyRegistry(), new InMemoryRequestContextFactory(), new NullSerializer());
        }

        [Fact]
        public async Task ExecutesQueries()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new TestQueryHandler();

            _handlerRegistry.Register<TestQueryA, TestQueryA.Response, TestQueryHandler>();
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

            var handlerA = new Mock<IQueryHandler<TestQueryA, TestQueryA.Response>>();
            var handlerB = new Mock<IQueryHandler<TestQueryB, TestQueryB.Response>>();

            _handlerRegistry.Register<TestQueryA, TestQueryA.Response, IQueryHandler<TestQueryA, TestQueryA.Response>>();
            _handlerRegistry.Register<TestQueryB, TestQueryB.Response, IQueryHandler<TestQueryB, TestQueryB.Response>>();

            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(IQueryHandler<TestQueryA, TestQueryA.Response>))).Returns(handlerA.Object);
            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(IQueryHandler<TestQueryB, TestQueryB.Response>))).Returns(handlerB.Object);

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

            var handlerA = new Mock<IQueryHandler<TestQueryA, TestQueryA.Response>>();
            handlerA.Setup(x => x.ExecuteAsync(It.Is<TestQueryA>(q => q.Id == id), default(CancellationToken))).Throws<FormatException>();

            _handlerRegistry.Register<TestQueryA, TestQueryA.Response, IQueryHandler<TestQueryA, TestQueryA.Response>>();

            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(IQueryHandler<TestQueryA, TestQueryA.Response>))).Returns(handlerA.Object);

            // Act
            await Assert.ThrowsAsync<FormatException>(async () => await _queryProcessor.ExecuteAsync(new TestQueryA(id)));

            // Assert
            handlerA.Verify(x => x.FallbackAsync(It.IsAny<TestQueryA>(), default(CancellationToken)), Times.Never);
        }

        public class TestQueryA : IQueryRequest<TestQueryA.Response>
        {
            public Guid Id { get; }

            public TestQueryA(Guid id)
            {
                Id = id;
            }

            public class Response : IQueryResponse
            {
                public Guid Id { get; }

                public Response(Guid id)
                {
                    Id = id;
                }
            }
        }

        public class TestQueryB : IQueryRequest<TestQueryB.Response>
        {
            public class Response : IQueryResponse { }
        }

        public class TestQueryHandler : AsyncQueryHandler<TestQueryA, TestQueryA.Response>
        {
            public override Task<TestQueryA.Response> ExecuteAsync(TestQueryA request, CancellationToken cancellationToken = default(CancellationToken))
            {
                Context.Bag.Add("id", request.Id);
                return Task.FromResult(new TestQueryA.Response(request.Id));
            }
        }
    }
}