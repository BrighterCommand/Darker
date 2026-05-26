using System;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Policies;
using Paramore.Darker.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class When_policy_decorator_executes_without_policies_should_throw_ConfigurationException
    {
        [Fact]
        public void Execute_without_policies_configured_should_throw_ConfigurationException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new RetryableQueryHandler();

            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, RetryableQueryHandler>();

            var handlerFactory = new SimpleHandlerFactory(type => handler);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(
                type => new RetryableQueryDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            // No policy registry passed — Context.Policies will be null
            var queryProcessor = new QueryProcessor(
                handlerConfiguration,
                new InMemoryQueryContextFactory());

            // Act & Assert — decorator must throw ConfigurationException when Context.Policies is null
            Should.Throw<ConfigurationException>(() =>
                queryProcessor.Execute(new SyncTestQuery(id)));
        }
    }
}
