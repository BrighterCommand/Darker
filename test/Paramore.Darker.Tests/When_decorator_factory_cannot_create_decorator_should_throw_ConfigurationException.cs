using System;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class DecoratorNotFoundTests
    {
        [Fact]
        public void When_sync_decorator_factory_returns_null_should_throw_ConfigurationException()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithFallback>();

            var handlerFactory = new SimpleHandlerFactory(type => new SyncHandlerWithFallback());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type => null!);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var queryProcessor = new QueryProcessor(
                new HandlerConfiguration(registry, handlerFactory, decoratorRegistry, decoratorFactory),
                new InMemoryQueryContextFactory());

            var query = new SyncTestQuery(Guid.NewGuid());

            // Act
            var exception = Should.Throw<ConfigurationException>(() => queryProcessor.Execute(query));

            // Assert
            exception.Message.ShouldContain("decorator");
            exception.Message.ShouldContain("could not be created");
        }

        [Fact]
        public void When_async_decorator_factory_returns_null_should_throw_ConfigurationException()
        {
            // Arrange
            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncHandlerWithFallback>();

            var handlerFactory = new SimpleHandlerFactory(type => null!);
            var asyncHandlerFactory = new SimpleHandlerFactory(type => new AsyncHandlerWithFallback());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type => null!);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var queryProcessor = new QueryProcessor(
                new HandlerConfiguration(
                    syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                    asyncRegistry, asyncHandlerFactory, decoratorRegistry, decoratorFactory),
                new InMemoryQueryContextFactory());

            var query = new AsyncTestQuery(Guid.NewGuid());

            // Act
            var exception = Should.Throw<ConfigurationException>(
                async () => await queryProcessor.ExecuteAsync(query));

            // Assert
            exception.Message.ShouldContain("decorator");
            exception.Message.ShouldContain("could not be created");
        }
    }
}
