using System;
using Darker.Attributes;
using Darker.Decorators;
using Moq;
using Shouldly;
using Xunit;

namespace Darker.Tests.Decorators
{
    public class FallbackPolicyTests
    {
        private readonly Mock<IQueryHandlerFactory> _handlerFactory;
        private readonly Mock<IQueryHandlerDecoratorFactory> _decoratorFactory;
        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryProcessor _queryProcessor;

        public FallbackPolicyTests()
        {
            _handlerFactory = new Mock<IQueryHandlerFactory>();
            _decoratorFactory = new Mock<IQueryHandlerDecoratorFactory>();
            _handlerRegistry = new QueryHandlerRegistry();

            var handlerConfiguration = new HandlerConfiguration(_handlerRegistry, _handlerFactory.Object, _decoratorFactory.Object);
            _queryProcessor = new QueryProcessor(handlerConfiguration, new PolicyRegistry(), new InMemoryRequestContextFactory());
        }

        [Fact]
        public void ExecutesFallbackWhenExceptionIsThrown()
        {
            // Arrange
            var handler = new TestQueryHandlerWithCatchAllFallback();
            var decorator = new FallbackPolicyDecorator<IQueryRequest<TestQuery.Response>, TestQuery.Response>();

            _handlerRegistry.Register<TestQuery, TestQuery.Response, TestQueryHandlerWithCatchAllFallback>();
            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(TestQueryHandlerWithCatchAllFallback))).Returns(handler);
            
            var decoratorType = typeof(FallbackPolicyDecorator<IQueryRequest<TestQuery.Response>, TestQuery.Response>);
            _decoratorFactory.Setup(x => x.Create<IQueryHandlerDecorator<IQueryRequest<TestQuery.Response>, TestQuery.Response>>(decoratorType)).Returns(decorator);

            // Act
            var response = _queryProcessor.Execute(new TestQuery());

            // Assert
            response.ShouldNotBeNull();
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag["Fallback_Exception_Cause"].ShouldBeAssignableTo<FormatException>();
            handler.Context.Bag.ShouldContainKeyAndValue("Check1", true);
            handler.Context.Bag.ShouldContainKeyAndValue("Check2", true);
        }

        [Fact]
        public void ExecutesFallbackWhenExceptionIsThrownAndExceptionTypeIsContainedInFilter()
        {
            // Arrange
            var handler = new TestQueryHandlerWithFormatExceptionFallback();
            var decorator = new FallbackPolicyDecorator<IQueryRequest<TestQuery.Response>, TestQuery.Response>();

            _handlerRegistry.Register<TestQuery, TestQuery.Response, TestQueryHandlerWithFormatExceptionFallback>();
            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(TestQueryHandlerWithFormatExceptionFallback))).Returns(handler);

            var decoratorType = typeof(FallbackPolicyDecorator<IQueryRequest<TestQuery.Response>, TestQuery.Response>);
            _decoratorFactory.Setup(x => x.Create<IQueryHandlerDecorator<IQueryRequest<TestQuery.Response>, TestQuery.Response>>(decoratorType)).Returns(decorator);

            // Act
            var response = _queryProcessor.Execute(new TestQuery());

            // Assert
            response.ShouldNotBeNull();
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag["Fallback_Exception_Cause"].ShouldBeAssignableTo<FormatException>();
            handler.Context.Bag.ShouldContainKeyAndValue("Check1", true);
            handler.Context.Bag.ShouldContainKeyAndValue("Check2", true);
        }

        [Fact]
        public void DoesNotExecuteFallbackWhenExceptionIsThrownAndExceptionTypeIsNotContainedInFilter()
        {
            // Arrange
            var handler = new TestQueryHandlerWithoutFormatExceptionFallback();
            var decorator = new FallbackPolicyDecorator<IQueryRequest<TestQuery.Response>, TestQuery.Response>();

            _handlerRegistry.Register<TestQuery, TestQuery.Response, TestQueryHandlerWithoutFormatExceptionFallback>();
            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(TestQueryHandlerWithoutFormatExceptionFallback))).Returns(handler);

            var decoratorType = typeof(FallbackPolicyDecorator<IQueryRequest<TestQuery.Response>, TestQuery.Response>);
            _decoratorFactory.Setup(x => x.Create<IQueryHandlerDecorator<IQueryRequest<TestQuery.Response>, TestQuery.Response>>(decoratorType)).Returns(decorator);

            // Act
            Assert.Throws<FormatException>(() => _queryProcessor.Execute(new TestQuery()));

            // Assert
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag.ShouldNotContainKey("Fallback_Exception_Cause");
            handler.Context.Bag.ShouldContainKeyAndValue("Check1", true);
            handler.Context.Bag.ShouldNotContainKey("Check2");
        }

        public class TestQuery : IQueryRequest<TestQuery.Response>
        {
            public class Response : IQueryResponse { }
        }

        public class TestQueryHandlerWithCatchAllFallback : QueryHandler<TestQuery, TestQuery.Response>
        {
            [FallbackPolicy(1)]
            public override TestQuery.Response Execute(TestQuery request)
            {
                Context.Bag.Add("Check1", true);
                throw new FormatException();
            }

            public override TestQuery.Response Fallback(TestQuery request)
            {
                Context.Bag.Add("Check2", true);
                return new TestQuery.Response();
            }
        }

        public class TestQueryHandlerWithFormatExceptionFallback : QueryHandler<TestQuery, TestQuery.Response>
        {
            [FallbackPolicy(1, typeof(AccessViolationException), typeof(FormatException))]
            public override TestQuery.Response Execute(TestQuery request)
            {
                Context.Bag.Add("Check1", true);
                throw new FormatException();
            }

            public override TestQuery.Response Fallback(TestQuery request)
            {
                Context.Bag.Add("Check2", true);
                return new TestQuery.Response();
            }
        }

        public class TestQueryHandlerWithoutFormatExceptionFallback : QueryHandler<TestQuery, TestQuery.Response>
        {
            [FallbackPolicy(1, typeof(AccessViolationException))]
            public override TestQuery.Response Execute(TestQuery request)
            {
                Context.Bag.Add("Check1", true);
                throw new FormatException();
            }

            public override TestQuery.Response Fallback(TestQuery request)
            {
                Context.Bag.Add("Check2", true);
                return new TestQuery.Response();
            }
        }
    }
}