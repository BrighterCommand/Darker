using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Moq;
using Shouldly;
using Xunit;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies.Attributes;
using Paramore.Darker.Policies.Handlers;

namespace Paramore.Darker.Core.Tests
{
    public class PipelineBuilderExceptionTests
    {
        private readonly Mock<IQueryHandlerFactory> _handlerFactory;
        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryProcessor _queryProcessor;
        private readonly Mock<IQueryHandlerDecoratorFactory> _decoratorFactory;

        public PipelineBuilderExceptionTests()
        {
            _handlerRegistry = new QueryHandlerRegistry();
            _handlerFactory = new Mock<IQueryHandlerFactory>();
            _decoratorFactory = new Mock<IQueryHandlerDecoratorFactory>();
            var decoratorRegistry = new Mock<IQueryHandlerDecoratorRegistry>();

            // Register the decorators
            decoratorRegistry.Setup(x => x.Register(typeof(FallbackPolicyDecorator<,>)));
            decoratorRegistry.Setup(x => x.Register(typeof(TestExceptionDecorator<,>)));

            var handlerConfiguration = new HandlerConfiguration(
                _handlerRegistry,
                _handlerFactory.Object,
                decoratorRegistry.Object,
                _decoratorFactory.Object);

            _queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());
        }

        [Fact]
        public void ShouldPreserveOriginalExceptionWhenHandlerThrowsException()
        {
            // Arrange
            _handlerRegistry.Register<ExceptionQuery, ExceptionQuery.Result, ExceptionQueryHandler>();
            _handlerFactory.Setup(x => x.Create(typeof(ExceptionQueryHandler))).Returns(new ExceptionQueryHandler());
            var query = new ExceptionQuery();

            // Act & Assert
            var exception = Should.Throw<InvalidOperationException>(() => _queryProcessor.Execute(query));
            exception.Message.ShouldBe("Test exception from Execute");
            _handlerFactory.Verify(x => x.Release(It.IsAny<ExceptionQueryHandler>()), Times.Once);
        }

        [Fact]
        public void ShouldThrowNullReferenceExceptionWhenInnerExceptionIsNull()
        {
            // Arrange
            _handlerRegistry.Register<NullInnerExceptionQuery, string, NullInnerExceptionQueryHandler>();
            _handlerFactory.Setup(x => x.Create(typeof(NullInnerExceptionQueryHandler))).Returns(new NullInnerExceptionQueryHandler());
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
            _handlerFactory.Setup(x => x.Create(typeof(FallbackExceptionQueryHandler))).Returns(new FallbackExceptionQueryHandler());
            var decoratorType = typeof(FallbackPolicyDecorator<IQuery<FallbackExceptionQuery.Result>, FallbackExceptionQuery.Result>);
            _decoratorFactory.Setup(x =>
                x.Create<IQueryHandlerDecorator<IQuery<FallbackExceptionQuery.Result>, FallbackExceptionQuery.Result>>(
                    decoratorType)).Returns(decorator);
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
            _handlerFactory.Setup(x => x.Create(typeof(DecoratorExceptionQueryHandler))).Returns(new DecoratorExceptionQueryHandler());
            var decoratorType = typeof(TestExceptionDecorator<IQuery<DecoratorExceptionQuery.Result>, DecoratorExceptionQuery.Result>);
            _decoratorFactory.Setup(x =>
                x.Create<IQueryHandlerDecorator<IQuery<DecoratorExceptionQuery.Result>, DecoratorExceptionQuery.Result>>(
                    decoratorType)).Returns(decorator);
            var query = new DecoratorExceptionQuery();


            // Act & Assert
            var exception = Should.Throw<InvalidOperationException>(() => _queryProcessor.Execute(query));
            exception.Message.ShouldBe("Test exception from decorator");
        }
    }
}
