using System;
using System.Threading.Tasks;
using Paramore.Darker.Policies.Handlers;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class AttributeMismatchTests
    {
        [Fact]
        public async Task When_sync_attribute_on_async_handler_should_throw_configuration_exception()
        {
            // Arrange — async handler with [FallbackPolicy] (sync attribute) on ExecuteAsync
            var handler = new AsyncHandlerWithSyncAttribute();

            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncHandlerWithSyncAttribute>();

            var handlerFactory = new SimpleHandlerFactory(type => handler);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type =>
                new FallbackPolicyDecorator<IQuery<AsyncTestQuery.Result>, AsyncTestQuery.Result>());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // Act
            var exception = await Should.ThrowAsync<ConfigurationException>(
                () => queryProcessor.ExecuteAsync(new AsyncTestQuery(Guid.NewGuid())));

            // Assert
            exception.Message.ShouldContain("sync");
            exception.Message.ShouldContain("async");
        }

        [Fact]
        public void When_async_attribute_on_sync_handler_should_throw_configuration_exception()
        {
            // Arrange — sync handler with [FallbackPolicyAttributeAsync] (async attribute) on Execute
            var handler = new SyncHandlerWithAsyncAttribute();

            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithAsyncAttribute>();

            var handlerFactory = new SimpleHandlerFactory(type => handler);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type =>
                new FallbackPolicyDecoratorAsync<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // Act
            var exception = Should.Throw<ConfigurationException>(
                () => queryProcessor.Execute(new SyncTestQuery(Guid.NewGuid())));

            // Assert
            exception.Message.ShouldContain("async");
            exception.Message.ShouldContain("sync");
        }
    }
}
