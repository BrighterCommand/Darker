using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class When_executing_async_query_with_provided_context_should_use_that_context
    {
        [Fact]
        public async Task ExecuteAsync_with_provided_context_should_use_that_context()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new AsyncContextCapturingHandler();

            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncContextCapturingHandler>();

            var handlerFactory = new SimpleHandlerFactory(type => handler);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var handlerConfiguration = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            var queryProcessor = new QueryProcessor(handlerConfiguration, new InMemoryQueryContextFactory());

            // A caller-provided context with a known Bag entry
            var callerContext = new QueryContext
            {
                Bag = new Dictionary<string, object> { { "trace-id", "xyz-789" } }
            };

            // Act — call ExecuteAsync with caller-provided context
            var result = await queryProcessor.ExecuteAsync(new AsyncTestQuery(id), queryContext: callerContext, cancellationToken: CancellationToken.None);

            // Assert — handler received the exact same context instance, Bag entry is accessible
            result.ShouldNotBeNull();
            result.Value.ShouldBe(id);
            handler.CapturedContext.ShouldBeSameAs(callerContext);
            handler.CapturedContext.Bag.ShouldContainKeyAndValue("trace-id", "xyz-789");
        }
    }
}
