using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shouldly;
using Xunit;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies.Handlers;

namespace Paramore.Darker.Core.Tests
{
    public class PipelineBuilderExceptionTests
    {
        private readonly Dictionary<Type, IQueryHandler> _handlers = new Dictionary<Type, IQueryHandler>();
        private readonly Dictionary<Type, IQueryHandlerDecorator> _decorators = new Dictionary<Type, IQueryHandlerDecorator>();
        private readonly RecordingHandlerFactory _handlerFactory;
        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryProcessor _queryProcessor;

        public PipelineBuilderExceptionTests()
        {
            _handlerRegistry = new QueryHandlerRegistry();
            _handlerFactory = new RecordingHandlerFactory(handlerType => _handlers[handlerType]);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(decoratorType => _decorators[decoratorType]);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            // Register the decorators
            decoratorRegistry.Register(typeof(FallbackPolicyDecorator<,>));
            decoratorRegistry.Register(typeof(TestExceptionDecorator<,>));

            var handlerConfiguration = new HandlerConfiguration(
                _handlerRegistry,
                _handlerFactory,
                decoratorRegistry,
                decoratorFactory);

            _queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());
        }

        [Fact]
        public void ShouldPreserveOriginalExceptionWhenHandlerThrowsException()
        {
            // Arrange
            _handlerRegistry.Register<ExceptionQuery, ExceptionQuery.Result, ExceptionQueryHandler>();
            _handlers[typeof(ExceptionQueryHandler)] = new ExceptionQueryHandler();
            var query = new ExceptionQuery();

            // Act & Assert
            var exception = Should.Throw<InvalidOperationException>(() => _queryProcessor.Execute(query));
            exception.Message.ShouldBe("Test exception from Execute");
            _handlerFactory.Released.OfType<ExceptionQueryHandler>().Count().ShouldBe(1);
        }

        [Fact]
        public void ShouldThrowNullReferenceExceptionWhenInnerExceptionIsNull()
        {
            // Arrange
            _handlerRegistry.Register<NullInnerExceptionQuery, string, NullInnerExceptionQueryHandler>();
            _handlers[typeof(NullInnerExceptionQueryHandler)] = new NullInnerExceptionQueryHandler();
            var query = new NullInnerExceptionQuery();

            // Act & Assert
            var exception = Should.Throw<TargetInvocationException>(() => _queryProcessor.Execute(query));
            exception.InnerException.ShouldBeNull();
        }

        [Fact]
        public void ShouldThrowExceptionWhenFallbackThrowsException()
        {

            // Arrange
            var decorator = new FallbackPolicyDecorator<IQuery<FallbackExceptionQuery.Result>, FallbackExceptionQuery.Result>();
            _handlerRegistry.Register<FallbackExceptionQuery, FallbackExceptionQuery.Result, FallbackExceptionQueryHandler>();
            _handlers[typeof(FallbackExceptionQueryHandler)] = new FallbackExceptionQueryHandler();
            var decoratorType = typeof(FallbackPolicyDecorator<IQuery<FallbackExceptionQuery.Result>, FallbackExceptionQuery.Result>);
            _decorators[decoratorType] = decorator;
            var query = new FallbackExceptionQuery();

            // Act & Assert
            var exception = Should.Throw<NotSupportedException>(() => _queryProcessor.Execute(query));
            exception.Message.ShouldBe("Test exception from Fallback");
        }

        [Fact]
        public void ShouldThrowExceptionWhenDecoratorThrowsException()
        {
            var decorator = new TestExceptionDecorator<IQuery<DecoratorExceptionQuery.Result>, DecoratorExceptionQuery.Result>();
            _handlerRegistry.Register<DecoratorExceptionQuery, DecoratorExceptionQuery.Result, DecoratorExceptionQueryHandler>();
            _handlers[typeof(DecoratorExceptionQueryHandler)] = new DecoratorExceptionQueryHandler();
            var decoratorType = typeof(TestExceptionDecorator<IQuery<DecoratorExceptionQuery.Result>, DecoratorExceptionQuery.Result>);
            _decorators[decoratorType] = decorator;
            var query = new DecoratorExceptionQuery();


            // Act & Assert
            var exception = Should.Throw<InvalidOperationException>(() => _queryProcessor.Execute(query));
            exception.Message.ShouldBe("Test exception from decorator");
        }
    }
}
