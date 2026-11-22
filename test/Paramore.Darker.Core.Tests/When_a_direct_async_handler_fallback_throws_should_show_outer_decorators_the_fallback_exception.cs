using System;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class ReflectiveFallbackExceptionAsyncTests
    {
        [Fact]
        public async Task When_a_direct_async_handler_fallback_throws_should_show_outer_decorators_the_fallback_exception()
        {
            // Arrange — the handler implements IQueryHandlerAsync<,> directly and its FallbackAsync throws before returning a Task;
            // an observer at step 1 sits outside [FallbackPolicyAttributeAsync(2)]
            var observer = new ExceptionObservingAsyncDecorator<IQuery<AsyncTestQuery.Result>, AsyncTestQuery.Result>();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, ThrowingFallbackAsyncHandler>();
            var handlerFactory = new SimpleHandlerFactory(_ => new ThrowingFallbackAsyncHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type =>
                type.GetGenericTypeDefinition() == typeof(ExceptionObservingAsyncDecorator<,>)
                    ? observer
                    : new FallbackPolicyDecoratorAsync<IQuery<AsyncTestQuery.Result>, AsyncTestQuery.Result>());
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var configuration = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);
            var queryProcessor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());

            // Act
            await Should.ThrowAsync<NotSupportedException>(() => queryProcessor.ExecuteAsync(
                new AsyncTestQuery(Guid.NewGuid()), cancellationToken: TestContext.Current.CancellationToken));

            // Assert — the outer decorator sees the fallback's own exception, not a TargetInvocationException
            observer.Observed.ShouldBeOfType<NotSupportedException>();
        }
    }
}
