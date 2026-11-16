using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class ExplicitFallbackSyncTests
    {
        [Fact]
        public void When_a_sync_handler_implements_fallback_explicitly_should_return_the_fallback_result()
        {
            // Arrange — Execute throws under [FallbackPolicy]; IQueryHandler<,>.Fallback is implemented explicitly
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, ExplicitFallbackSyncHandler>();
            var handlerFactory = new SimpleHandlerFactory(_ => new ExplicitFallbackSyncHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                new FallbackPolicyDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>());
            var configuration = new HandlerConfiguration(
                registry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);
            var queryProcessor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());
            var queryId = Guid.NewGuid();

            // Act
            var result = queryProcessor.Execute(new SyncTestQuery(queryId));

            // Assert — only the explicit Fallback returns the query's id
            result.Value.ShouldBe(queryId);
        }
    }
}
