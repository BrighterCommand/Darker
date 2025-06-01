using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Moq;
using Shouldly;
using Xunit;
using Paramore.Darker.Attributes;
using Paramore.Darker.Decorators;

namespace Paramore.Darker.Tests
{
    public class PipelineBuilderExceptionTests
    {
        private readonly Mock<IQueryHandlerFactory> _handlerFactory;
        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryProcessor _queryProcessor;
        private readonly Mock<IQueryHandlerDecoratorFactory> _decoratorFactory;

        // Query and handler for normal exception scenario
        private class ExceptionQuery : IQuery<ExceptionQuery.Result>
        {
            public class Result { }
        }
        private class ExceptionQueryHandler : QueryHandler<ExceptionQuery, ExceptionQuery.Result>
        {
            public override ExceptionQuery.Result Execute(ExceptionQuery query)
            {
                throw new InvalidOperationException("Test exception from Execute");
            }
            public override Task<ExceptionQuery.Result> ExecuteAsync(ExceptionQuery query, CancellationToken cancellationToken = default)
            {
                throw new ArgumentException("Test exception from ExecuteAsync");
            }
        }

        // Query and handler for fallback exception scenario
        private class FallbackExceptionQuery : IQuery<FallbackExceptionQuery.Result>
        {
            public class Result { }
        }
        private class FallbackExceptionQueryHandler : QueryHandler<FallbackExceptionQuery, FallbackExceptionQuery.Result>
        {
            [FallbackPolicy(step: 1, typeof(InvalidOperationException))]
            public override FallbackExceptionQuery.Result Execute(FallbackExceptionQuery query)
            {
                throw new InvalidOperationException("Test exception from Execute");
            }
            public override FallbackExceptionQuery.Result Fallback(FallbackExceptionQuery query)
            {
                throw new NotSupportedException("Test exception from Fallback");
            }
        }

        // Query and handler for null inner exception scenario
        private class NullInnerExceptionQuery : IQuery<string> { }
        private class NullInnerExceptionQueryHandler : QueryHandler<NullInnerExceptionQuery, string>
        {
            public override string Execute(NullInnerExceptionQuery query)
            {
                throw new TargetInvocationException(null);
            }
        }

        // Query and handler for decorator exception scenario
        private class DecoratorExceptionQuery : IQuery<DecoratorExceptionQuery.Result>
        {
            public class Result { }
        }
        private class DecoratorExceptionQueryHandler : QueryHandler<DecoratorExceptionQuery, DecoratorExceptionQuery.Result>
        {
            [DecoratorException(step: 1)]
            public override DecoratorExceptionQuery.Result Execute(DecoratorExceptionQuery query)
            {
                return new DecoratorExceptionQuery.Result();
            }
        }
        private class TestExceptionDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
            where TQuery : IQuery<TResult>
        {
            public IQueryContext Context { get; set; }
            public void InitializeFromAttributeParams(object[] attributeParams) { }
            public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
            {
                throw new InvalidOperationException("Test exception from decorator");
            }

            public Task<TResult> ExecuteAsync(TQuery query, Func<TQuery, CancellationToken, Task<TResult>> next, Func<TQuery, CancellationToken, Task<TResult>> fallback,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new InvalidOperationException("Test exception from async decorator");
            }
            
        }
        
        
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class DecoratorExceptionAttribute : QueryHandlerAttribute
        {

            public DecoratorExceptionAttribute(int step) : base(step)
            {
            }

            public override object[] GetAttributeParams()
            {
                return [];
            }

            public override Type GetDecoratorType()
            {
                return typeof(TestExceptionDecorator<,>);
            }
        }

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
        public async Task ShouldPreserveOriginalExceptionWhenHandlerThrowsExceptionAsync()
        {
            // Arrange
            _handlerRegistry.Register<ExceptionQuery, ExceptionQuery.Result, ExceptionQueryHandler>();
            _handlerFactory.Setup(x => x.Create(typeof(ExceptionQueryHandler))).Returns(new ExceptionQueryHandler());
            var query = new ExceptionQuery();

            // Act & Assert
            var exception = await Should.ThrowAsync<ArgumentException>(async () =>
                await _queryProcessor.ExecuteAsync(query));
            exception.Message.ShouldBe("Test exception from ExecuteAsync");
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
            var exception = Should.Throw<NullReferenceException>(() => _queryProcessor.Execute(query));
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