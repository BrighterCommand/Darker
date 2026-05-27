using System;
using Paramore.Darker.Policies.Handlers;
using Paramore.Darker.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class SyncPipelineTests
    {
        [Fact]
        public void When_sync_query_executed_should_resolve_from_sync_registry_and_build_sync_decorator_chain()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new SyncHandlerWithFallback();

            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithFallback>();

            var handlerFactory = new SimpleHandlerFactory(type => handler);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type =>
                new FallbackPolicyDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // Act
            var result = queryProcessor.Execute(new SyncTestQuery(id));

            // Assert — sync pipeline resolved, decorator chain ran, fallback returned result
            result.ShouldNotBeNull();
            result.Value.ShouldBe(id);
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag.ShouldContainKeyAndValue("executed", true);
            handler.Context.Bag.ShouldContainKeyAndValue("fell-back", true);
            handler.Context.Bag["Fallback_Exception_Cause"].ShouldBeAssignableTo<InvalidOperationException>();
        }
    }
}
