using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class ExplicitExecuteStreamTests
    {
        [Fact]
        public async Task When_a_stream_handler_implements_execute_explicitly_should_throw_ConfigurationException()
        {
            // Arrange — IStreamQueryHandler<,>.ExecuteAsync is implemented explicitly, so the handler has no public ExecuteAsync
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<StreamTestQuery, string, ExplicitInterfaceStreamHandler>();
            var handlerFactory = new SimpleHandlerFactory(_ => new ExplicitInterfaceStreamHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var configuration = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);
            var queryProcessor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());

            // Act
            var exception = await Should.ThrowAsync<ConfigurationException>(async () =>
            {
                await foreach (var _ in queryProcessor.ExecuteStream(new StreamTestQuery(), cancellationToken: TestContext.Current.CancellationToken)) { }
            });

            // Assert — names the handler and says the interface is implemented only explicitly
            exception.Message.ShouldContain(typeof(ExplicitInterfaceStreamHandler).FullName!);
            exception.Message.ShouldContain("'IStreamQueryHandler<,>' only explicitly");
        }
    }
}
