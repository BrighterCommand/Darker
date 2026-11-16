using System;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class ExplicitFallbackAsyncTests
    {
        [Fact]
        public async Task When_an_async_handler_implements_fallback_explicitly_should_return_the_fallback_result()
        {
            // Arrange — ExecuteAsync throws under [FallbackPolicyAttributeAsync]; IQueryHandlerAsync<,>.FallbackAsync is implemented explicitly
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, ExplicitFallbackAsyncHandler>();
            var handlerFactory = new SimpleHandlerFactory(_ => new ExplicitFallbackAsyncHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                new FallbackPolicyDecoratorAsync<IQuery<AsyncTestQuery.Result>, AsyncTestQuery.Result>());
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var configuration = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);
            var queryProcessor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());
            var queryId = Guid.NewGuid();

            // Act
            var result = await queryProcessor.ExecuteAsync(new AsyncTestQuery(queryId), cancellationToken: TestContext.Current.CancellationToken);

            // Assert — only the explicit FallbackAsync returns the query's id
            result.Value.ShouldBe(queryId);
        }
    }
}
