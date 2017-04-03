using System;
using Moq;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
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
            _queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());
        }

        [Fact]
        public void ExecutesQueries()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new TestQueryHandler();

            _handlerRegistry.Register<TestQueryA, Guid, TestQueryHandler>();
            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(TestQueryHandler))).Returns(handler);

            // Act
            var result = _queryProcessor.Execute(new TestQueryA(id));

            // Assert
            result.ShouldBe(id);
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag.ShouldContainKeyAndValue("id", id);
        }

        [Fact]
        public void ExecutesTheMatchingHandler()
        {
            // Arrange
            var id = Guid.NewGuid();

            var handlerA = new Mock<IQueryHandler<TestQueryA, Guid>>();
            var handlerB = new Mock<IQueryHandler<TestQueryB, object>>();

            _handlerRegistry.Register<TestQueryA, Guid, IQueryHandler<TestQueryA, Guid>>();
            _handlerRegistry.Register<TestQueryB, object, IQueryHandler<TestQueryB, object>>();

            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(IQueryHandler<TestQueryA, Guid>))).Returns(handlerA.Object);
            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(IQueryHandler<TestQueryB, object>))).Returns(handlerB.Object);

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

            var handlerA = new Mock<IQueryHandler<TestQueryA, Guid>>();
            handlerA.Setup(x => x.Execute(It.Is<TestQueryA>(q => q.Id == id))).Throws<FormatException>();

            _handlerRegistry.Register<TestQueryA, Guid, IQueryHandler<TestQueryA, Guid>>();

            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(IQueryHandler<TestQueryA, Guid>))).Returns(handlerA.Object);

            // Act
            Assert.Throws<FormatException>(() => _queryProcessor.Execute(new TestQueryA(id)));

            // Assert
            handlerA.Verify(x => x.Fallback(It.IsAny<TestQueryA>()), Times.Never);
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

        public class TestQueryHandler : QueryHandler<TestQueryA, Guid>
        {
            public override Guid Execute(TestQueryA query)
            {
                Context.Bag.Add("id", query.Id);
                return query.Id;
            }
        }
    }
}