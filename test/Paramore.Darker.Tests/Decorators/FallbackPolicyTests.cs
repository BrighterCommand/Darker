using System;
using Moq;
using Paramore.Darker.Attributes;
using Paramore.Darker.Decorators;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests.Decorators
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
            _queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());
        }

        [Fact]
        public void ExecutesFallbackWhenExceptionIsThrown()
        {
            // Arrange
            var handler = new TestQueryHandlerWithCatchAllFallback();
            var decorator = new FallbackPolicyDecorator<IQuery<TestQuery.Result>, TestQuery.Result>();

            _handlerRegistry.Register<TestQuery, TestQuery.Result, TestQueryHandlerWithCatchAllFallback>();
            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(TestQueryHandlerWithCatchAllFallback))).Returns(handler);
            
            var decoratorType = typeof(FallbackPolicyDecorator<IQuery<TestQuery.Result>, TestQuery.Result>);
            _decoratorFactory.Setup(x => x.Create<IQueryHandlerDecorator<IQuery<TestQuery.Result>, TestQuery.Result>>(decoratorType)).Returns(decorator);

            // Act
            var result = _queryProcessor.Execute(new TestQuery());

            // Assert
            result.ShouldNotBeNull();
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
            var decorator = new FallbackPolicyDecorator<IQuery<TestQuery.Result>, TestQuery.Result>();

            _handlerRegistry.Register<TestQuery, TestQuery.Result, TestQueryHandlerWithFormatExceptionFallback>();
            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(TestQueryHandlerWithFormatExceptionFallback))).Returns(handler);

            var decoratorType = typeof(FallbackPolicyDecorator<IQuery<TestQuery.Result>, TestQuery.Result>);
            _decoratorFactory.Setup(x => x.Create<IQueryHandlerDecorator<IQuery<TestQuery.Result>, TestQuery.Result>>(decoratorType)).Returns(decorator);

            // Act
            var result = _queryProcessor.Execute(new TestQuery());

            // Assert
            result.ShouldNotBeNull();
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
            var decorator = new FallbackPolicyDecorator<IQuery<TestQuery.Result>, TestQuery.Result>();

            _handlerRegistry.Register<TestQuery, TestQuery.Result, TestQueryHandlerWithoutFormatExceptionFallback>();
            _handlerFactory.Setup(x => x.Create<dynamic>(typeof(TestQueryHandlerWithoutFormatExceptionFallback))).Returns(handler);

            var decoratorType = typeof(FallbackPolicyDecorator<IQuery<TestQuery.Result>, TestQuery.Result>);
            _decoratorFactory.Setup(x => x.Create<IQueryHandlerDecorator<IQuery<TestQuery.Result>, TestQuery.Result>>(decoratorType)).Returns(decorator);

            // Act
            Assert.Throws<FormatException>(() => _queryProcessor.Execute(new TestQuery()));

            // Assert
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag.ShouldNotContainKey("Fallback_Exception_Cause");
            handler.Context.Bag.ShouldContainKeyAndValue("Check1", true);
            handler.Context.Bag.ShouldNotContainKey("Check2");
        }

        public class TestQuery : IQuery<TestQuery.Result>
        {
            public class Result { }
        }

        public class TestQueryHandlerWithCatchAllFallback : QueryHandler<TestQuery, TestQuery.Result>
        {
            [FallbackPolicy(1)]
            public override TestQuery.Result Execute(TestQuery query)
            {
                Context.Bag.Add("Check1", true);
                throw new FormatException();
            }

            public override TestQuery.Result Fallback(TestQuery query)
            {
                Context.Bag.Add("Check2", true);
                return new TestQuery.Result();
            }
        }

        public class TestQueryHandlerWithFormatExceptionFallback : QueryHandler<TestQuery, TestQuery.Result>
        {
            [FallbackPolicy(1, typeof(ArithmeticException), typeof(FormatException))]
            public override TestQuery.Result Execute(TestQuery query)
            {
                Context.Bag.Add("Check1", true);
                throw new FormatException();
            }

            public override TestQuery.Result Fallback(TestQuery query)
            {
                Context.Bag.Add("Check2", true);
                return new TestQuery.Result();
            }
        }

        public class TestQueryHandlerWithoutFormatExceptionFallback : QueryHandler<TestQuery, TestQuery.Result>
        {
            [FallbackPolicy(1, typeof(ArithmeticException))]
            public override TestQuery.Result Execute(TestQuery query)
            {
                Context.Bag.Add("Check1", true);
                throw new FormatException();
            }

            public override TestQuery.Result Fallback(TestQuery query)
            {
                Context.Bag.Add("Check2", true);
                return new TestQuery.Result();
            }
        }
    }
}