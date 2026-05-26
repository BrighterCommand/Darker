using System;
using Paramore.Darker.Exceptions;
using Paramore.Darker.QueryLogging;
using Paramore.Darker.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class When_logging_decorator_executes_without_settings_should_throw_ConfigurationException
    {
        [Fact]
        public void Execute_without_serializer_settings_should_throw_ConfigurationException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new LoggingQueryHandler();

            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, LoggingQueryHandler>();

            var handlerFactory = new SimpleHandlerFactory(type => handler);

            // Decorator created with null settings — no serializer available
            var decoratorFactory = new SimpleHandlerDecoratorFactory(
                type => new QueryLoggingDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>(serializerSettings: null));
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var queryProcessor = new QueryProcessor(
                handlerConfiguration,
                new InMemoryQueryContextFactory());

            // Act & Assert — decorator must throw ConfigurationException when no settings configured
            Should.Throw<ConfigurationException>(() =>
                queryProcessor.Execute(new SyncTestQuery(id)));
        }
    }
}
