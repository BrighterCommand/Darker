using System;
using System.Threading.Tasks;
using Paramore.Darker.Policies.Handlers;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class AsyncPipelineTests
    {
        [Fact]
        public async Task When_async_query_executed_should_resolve_from_async_registry_and_build_async_decorator_chain()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new AsyncHandlerWithFallback();

            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncHandlerWithFallback>();

            var handlerFactory = new SimpleHandlerFactory(type => handler);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type =>
                new FallbackPolicyDecoratorAsync<IQuery<AsyncTestQuery.Result>, AsyncTestQuery.Result>());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // Act
            var result = await queryProcessor.ExecuteAsync(new AsyncTestQuery(id));

            // Assert — async pipeline resolved, decorator chain ran, fallback returned result
            result.ShouldNotBeNull();
            result.Value.ShouldBe(id);
            handler.Context.ShouldNotBeNull();
            handler.Context.Bag.ShouldContainKeyAndValue("executed", true);
            handler.Context.Bag.ShouldContainKeyAndValue("fell-back", true);
            handler.Context.Bag["Fallback_Exception_Cause"].ShouldBeAssignableTo<InvalidOperationException>();
        }
    }
}
