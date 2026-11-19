using System;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.Exported;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class ExplicitExecuteAsyncTests
    {
        [Fact]
        public async Task When_an_async_handler_implements_execute_explicitly_should_throw_ConfigurationException()
        {
            // Arrange — IQueryHandlerAsync<,>.ExecuteAsync is implemented explicitly, so the handler has no public ExecuteAsync
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<TestQueryA, Guid, ExplicitInterfaceAsyncHandler>();
            var handlerFactory = new SimpleHandlerFactory(_ => new ExplicitInterfaceAsyncHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var configuration = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);
            var queryProcessor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());

            // Act
            var exception = await Should.ThrowAsync<ConfigurationException>(() =>
                queryProcessor.ExecuteAsync(new TestQueryA(Guid.NewGuid()), cancellationToken: TestContext.Current.CancellationToken));

            // Assert — names the handler and says the interface is implemented only explicitly
            exception.Message.ShouldContain(typeof(ExplicitInterfaceAsyncHandler).FullName!);
            exception.Message.ShouldContain("'IQueryHandlerAsync<,>' only explicitly");
        }
    }
}
