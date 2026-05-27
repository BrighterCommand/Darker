using System;
using Newtonsoft.Json;
using Paramore.Darker.Logging.Handlers;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_logging_decorator_executes_should_use_injected_serializer_settings
    {
        [Fact]
        public void Execute_with_injected_serializer_settings_should_log_and_return_result()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new LoggingQueryHandler();

            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, LoggingQueryHandler>();

            var handlerFactory = new SimpleHandlerFactory(type => handler);

            var serializerSettings = new JsonSerializerSettings { Formatting = Formatting.None };

            // Decorator created with injected settings — no Context.Bag entry required
            var decoratorFactory = new SimpleHandlerDecoratorFactory(
                type => new QueryLoggingDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>(serializerSettings));
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var queryProcessor = new QueryProcessor(
                handlerConfiguration,
                new InMemoryQueryContextFactory());

            // Act — Context.Bag intentionally has no serializer entry
            var result = queryProcessor.Execute(new SyncTestQuery(id));

            // Assert
            result.ShouldNotBeNull();
            result.Value.ShouldBe(id);
        }
    }
}
