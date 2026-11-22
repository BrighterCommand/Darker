using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class ReflectiveFallbackExceptionSyncTests
    {
        [Fact]
        public void When_a_direct_sync_handler_fallback_throws_should_show_outer_decorators_the_fallback_exception()
        {
            // Arrange — the handler implements IQueryHandler<,> directly; an observer at step 1 sits outside [FallbackPolicy(2)]
            var observer = new ExceptionObservingDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>();
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, ThrowingFallbackSyncHandler>();
            var handlerFactory = new SimpleHandlerFactory(_ => new ThrowingFallbackSyncHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type =>
                type.GetGenericTypeDefinition() == typeof(ExceptionObservingDecorator<,>)
                    ? observer
                    : new FallbackPolicyDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>());
            var configuration = new HandlerConfiguration(
                registry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);
            var queryProcessor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());

            // Act
            Should.Throw<NotSupportedException>(() => queryProcessor.Execute(new SyncTestQuery(Guid.NewGuid())));

            // Assert — the outer decorator sees the fallback's own exception, not a TargetInvocationException
            observer.Observed.ShouldBeOfType<NotSupportedException>();
        }
    }
}
