using System;
using Paramore.Darker.Core.Tests.Exported;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class ExplicitExecuteSyncTests
    {
        [Fact]
        public void When_a_sync_handler_implements_execute_explicitly_should_throw_ConfigurationException()
        {
            // Arrange — IQueryHandler<,>.Execute is implemented explicitly, so the handler has no public Execute
            var registry = new QueryHandlerRegistry();
            registry.Register<TestQueryA, Guid, ExplicitInterfaceSyncHandler>();
            var handlerFactory = new SimpleHandlerFactory(_ => new ExplicitInterfaceSyncHandler());
            var configuration = new HandlerConfiguration(
                registry, handlerFactory, new InMemoryDecoratorRegistry(), new SimpleHandlerDecoratorFactory(_ => null));
            var queryProcessor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());

            // Act
            var exception = Should.Throw<ConfigurationException>(() => queryProcessor.Execute(new TestQueryA(Guid.NewGuid())));

            // Assert — names the handler and says the interface is implemented only explicitly
            exception.Message.ShouldContain(typeof(ExplicitInterfaceSyncHandler).FullName!);
            exception.Message.ShouldContain("'IQueryHandler<,>' only explicitly");
        }
    }
}
