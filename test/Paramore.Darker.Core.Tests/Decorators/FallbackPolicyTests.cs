using System;
using System.Collections.Generic;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies.Attributes;
using Paramore.Darker.Policies.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Decorators
{
    public class FallbackPolicyTests
    {
        private readonly Dictionary<Type, IQueryHandler> _handlers = new Dictionary<Type, IQueryHandler>();
        private readonly Dictionary<Type, IQueryHandlerDecorator> _decorators = new Dictionary<Type, IQueryHandlerDecorator>();
        private readonly RecordingHandlerFactory _handlerFactory;
        private readonly RecordingDecoratorFactory _decoratorFactory;
        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryProcessor _queryProcessor;

        public FallbackPolicyTests()
        {
            _handlerRegistry = new QueryHandlerRegistry();
            _handlerFactory = new RecordingHandlerFactory(handlerType => _handlers[handlerType]);
            _decoratorFactory = new RecordingDecoratorFactory(decoratorType => _decorators[decoratorType]);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(_handlerRegistry, _handlerFactory, decoratorRegistry, _decoratorFactory);
            _queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());
        }

        [Fact]
        public void ExecutesFallbackWhenExceptionIsThrown()
        {
            // Arrange
            var handler = new TestQueryHandlerWithCatchAllFallback();
            var decorator = new FallbackPolicyDecorator<IQuery<TestQuery.Result>, TestQuery.Result>();

            _handlerRegistry.Register<TestQuery, TestQuery.Result, TestQueryHandlerWithCatchAllFallback>();
            _handlers[typeof(TestQueryHandlerWithCatchAllFallback)] = handler;

            var decoratorType = typeof(FallbackPolicyDecorator<IQuery<TestQuery.Result>, TestQuery.Result>);
            _decorators[decoratorType] = decorator;

            // Act
            var result = _queryProcessor.Execute(new TestQuery());

            // Assert
            result.ShouldNotBeNull();
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag["Fallback_Exception_Cause"].ShouldBeAssignableTo<FormatException>();
            handler.Context.Bag.ShouldContainKeyAndValue("Check1", true);
            handler.Context.Bag.ShouldContainKeyAndValue("Check2", true);
            _handlerFactory.ReleaseCount(handler).ShouldBe(1);
            _decoratorFactory.ReleaseCount(decorator).ShouldBe(1);
        }

        [Fact]
        public void ExecutesFallbackWhenExceptionIsThrownAndExceptionTypeIsContainedInFilter()
        {
            // Arrange
            var handler = new TestQueryHandlerWithFormatExceptionFallback();
            var decorator = new FallbackPolicyDecorator<IQuery<TestQuery.Result>, TestQuery.Result>();

            _handlerRegistry.Register<TestQuery, TestQuery.Result, TestQueryHandlerWithFormatExceptionFallback>();
            _handlers[typeof(TestQueryHandlerWithFormatExceptionFallback)] = handler;

            var decoratorType = typeof(FallbackPolicyDecorator<IQuery<TestQuery.Result>, TestQuery.Result>);
            _decorators[decoratorType] = decorator;

            // Act
            var result = _queryProcessor.Execute(new TestQuery());

            // Assert
            result.ShouldNotBeNull();
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag["Fallback_Exception_Cause"].ShouldBeAssignableTo<FormatException>();
            handler.Context.Bag.ShouldContainKeyAndValue("Check1", true);
            handler.Context.Bag.ShouldContainKeyAndValue("Check2", true);
            _handlerFactory.ReleaseCount(handler).ShouldBe(1);
            _decoratorFactory.ReleaseCount(decorator).ShouldBe(1);
        }

        [Fact]
        public void DoesNotExecuteFallbackWhenExceptionIsThrownAndExceptionTypeIsNotContainedInFilter()
        {
            // Arrange
            var handler = new TestQueryHandlerWithoutFormatExceptionFallback();
            var decorator = new FallbackPolicyDecorator<IQuery<TestQuery.Result>, TestQuery.Result>();

            _handlerRegistry.Register<TestQuery, TestQuery.Result, TestQueryHandlerWithoutFormatExceptionFallback>();
            _handlers[typeof(TestQueryHandlerWithoutFormatExceptionFallback)] = handler;

            var decoratorType = typeof(FallbackPolicyDecorator<IQuery<TestQuery.Result>, TestQuery.Result>);
            _decorators[decoratorType] = decorator;

            // Act
            Assert.Throws<FormatException>(() => _queryProcessor.Execute(new TestQuery()));

            // Assert
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag.ShouldNotContainKey("Fallback_Exception_Cause");
            handler.Context.Bag.ShouldContainKeyAndValue("Check1", true);
            handler.Context.Bag.ShouldNotContainKey("Check2");
            _handlerFactory.ReleaseCount(handler).ShouldBe(1);
            _decoratorFactory.ReleaseCount(decorator).ShouldBe(1);
        }
    }
}
