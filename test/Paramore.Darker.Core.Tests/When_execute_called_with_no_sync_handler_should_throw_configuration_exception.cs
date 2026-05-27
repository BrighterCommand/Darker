using System;
using System.Threading.Tasks;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class HandlerNotFoundTests
    {
        [Fact]
        public void When_execute_called_with_no_sync_handler_should_throw_configuration_exception()
        {
            // Arrange — async handler registered, but no sync handler
            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncHandlerWithFallback>();

            var handlerFactory = new SimpleHandlerFactory(type =>
                throw new InvalidOperationException("should not be called"));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type =>
                throw new InvalidOperationException("should not be called"));
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // Act
            var exception = Should.Throw<ConfigurationException>(
                () => queryProcessor.Execute(new AsyncTestQuery(Guid.NewGuid())));

            // Assert
            exception.Message.ShouldContain(nameof(AsyncTestQuery));
            exception.Message.ShouldContain("ExecuteAsync");
        }

        [Fact]
        public async Task When_execute_async_called_with_no_async_handler_should_throw_configuration_exception()
        {
            // Arrange — sync handler registered, but no async handler
            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithFallback>();

            var asyncRegistry = new QueryHandlerRegistryAsync();

            var handlerFactory = new SimpleHandlerFactory(type =>
                throw new InvalidOperationException("should not be called"));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type =>
                throw new InvalidOperationException("should not be called"));
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // Act
            var exception = await Should.ThrowAsync<ConfigurationException>(
                () => queryProcessor.ExecuteAsync(new SyncTestQuery(Guid.NewGuid())));

            // Assert
            exception.Message.ShouldContain(nameof(SyncTestQuery));
            exception.Message.ShouldContain("Execute");
        }
    }
}
