using System;
using System.Threading.Tasks;
using Paramore.Darker.Policies.Handlers;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class DualPathQueryProcessorTests
    {
        [Fact]
        public async Task When_query_processor_has_both_sync_and_async_handlers_should_dispatch_to_correct_pipeline()
        {
            // Arrange
            var syncId = Guid.NewGuid();
            var asyncId = Guid.NewGuid();
            var syncHandler = new SyncHandlerWithFallback();
            var asyncHandler = new AsyncHandlerWithFallback();

            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithFallback>();

            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncHandlerWithFallback>();

            var handlerFactory = new SimpleHandlerFactory(type =>
            {
                if (type == typeof(SyncHandlerWithFallback)) return syncHandler;
                if (type == typeof(AsyncHandlerWithFallback)) return asyncHandler;
                throw new InvalidOperationException($"Unknown handler type: {type}");
            });

            var decoratorFactory = new SimpleHandlerDecoratorFactory(type =>
            {
                if (type == typeof(FallbackPolicyDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>))
                    return new FallbackPolicyDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>();
                if (type == typeof(FallbackPolicyDecoratorAsync<IQuery<AsyncTestQuery.Result>, AsyncTestQuery.Result>))
                    return new FallbackPolicyDecoratorAsync<IQuery<AsyncTestQuery.Result>, AsyncTestQuery.Result>();
                throw new InvalidOperationException($"Unknown decorator type: {type}");
            });

            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // Act — execute both paths on the same processor
            var syncResult = queryProcessor.Execute(new SyncTestQuery(syncId));
            var asyncResult = await queryProcessor.ExecuteAsync(new AsyncTestQuery(asyncId));

            // Assert — sync path used sync handler
            syncResult.ShouldNotBeNull();
            syncResult.Value.ShouldBe(syncId);
            syncHandler.Context.ShouldNotBeNull();
            syncHandler.Context.Bag.ShouldContainKeyAndValue("executed", true);
            syncHandler.Context.Bag.ShouldContainKeyAndValue("fell-back", true);

            // Assert — async path used async handler
            asyncResult.ShouldNotBeNull();
            asyncResult.Value.ShouldBe(asyncId);
            asyncHandler.Context.ShouldNotBeNull();
            asyncHandler.Context.Bag.ShouldContainKeyAndValue("executed", true);
            asyncHandler.Context.Bag.ShouldContainKeyAndValue("fell-back", true);
        }
    }
}
