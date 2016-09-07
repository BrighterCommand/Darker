using System;
using Moq;
using Shouldly;
using Xunit;

namespace Darker.Tests
{
    public class QueryProcessorTests
    {
        private readonly Mock<IQueryHandlerFactory> _handlerFactory;
        private readonly Mock<IQueryHandlerDecoratorFactory> _decoratorFactory;
        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryProcessor _queryProcessor;

        public QueryProcessorTests()
        {
            _handlerFactory = new Mock<IQueryHandlerFactory>();
            _decoratorFactory = new Mock<IQueryHandlerDecoratorFactory>();
            _handlerRegistry = new QueryHandlerRegistry();

            var handlerConfiguration = new HandlerConfiguration(_handlerRegistry, _handlerFactory.Object, _decoratorFactory.Object);
            _queryProcessor = new QueryProcessor(handlerConfiguration, new PolicyRegistry(), new InMemoryRequestContextFactory());
        }

        [Fact]
        public void ExecutesQueries()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new TestQueryHandler();

            _handlerRegistry.Register<TestQueryA, TestQueryA.Response, TestQueryHandler>();
            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(TestQueryHandler))).Returns(handler);

            // Act
            var response = _queryProcessor.Execute(new TestQueryA(id));

            // Assert
            response.ShouldNotBeNull();
            response.Id.ShouldBe(id);
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag.ShouldContainKeyAndValue("id", id);
        }

        [Fact]
        public void ExecutesTheMatchingHandler()
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
            _queryProcessor.Execute(new TestQueryA(id));

            // Assert
            handlerA.Verify(x => x.Execute(It.Is<TestQueryA>(q => q.Id == id)), Times.Once);
            handlerA.Verify(x => x.Fallback(It.IsAny<TestQueryA>()), Times.Never);
            handlerB.Verify(x => x.Execute(It.IsAny<TestQueryB>()), Times.Never);
            handlerB.Verify(x => x.Fallback(It.IsAny<TestQueryB>()), Times.Never);
        }

        [Fact]
        public void ExceptionsDontCauseFallbackByDefault()
        {
            // Arrange
            var id = Guid.NewGuid();

            var handlerA = new Mock<IQueryHandler<TestQueryA, TestQueryA.Response>>();
            handlerA.Setup(x => x.Execute(It.Is<TestQueryA>(q => q.Id == id))).Throws<FormatException>();

            _handlerRegistry.Register<TestQueryA, TestQueryA.Response, IQueryHandler<TestQueryA, TestQueryA.Response>>();

            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(IQueryHandler<TestQueryA, TestQueryA.Response>))).Returns(handlerA.Object);

            // Act
            Assert.Throws<FormatException>(() => _queryProcessor.Execute(new TestQueryA(id)));

            // Assert
            handlerA.Verify(x => x.Fallback(It.IsAny<TestQueryA>()), Times.Never);
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

        public class TestQueryHandler : QueryHandler<TestQueryA, TestQueryA.Response>
        {
            public override TestQueryA.Response Execute(TestQueryA request)
            {
                Context.Bag.Add("id", request.Id);
                return new TestQueryA.Response(request.Id);
            }
        }
    }
}