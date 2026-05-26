using System;
using System.Collections.Generic;
using Paramore.Darker.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class When_executing_query_with_provided_context_should_use_that_context
    {
        [Fact]
        public void Execute_with_provided_context_should_use_that_context()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new ContextCapturingHandler();

            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, ContextCapturingHandler>();

            var handlerFactory = new SimpleHandlerFactory(type => handler);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // A caller-provided context with a known Bag entry
            var callerContext = new QueryContext
            {
                Bag = new Dictionary<string, object> { { "correlation-id", "abc-123" } }
            };

            // Act — call Execute with caller-provided context
            var result = queryProcessor.Execute(new SyncTestQuery(id), queryContext: callerContext);

            // Assert — handler received the exact same context instance, Bag entry is accessible
            result.ShouldNotBeNull();
            result.Value.ShouldBe(id);
            handler.CapturedContext.ShouldBeSameAs(callerContext);
            handler.CapturedContext.Bag.ShouldContainKeyAndValue("correlation-id", "abc-123");
        }
    }
}
