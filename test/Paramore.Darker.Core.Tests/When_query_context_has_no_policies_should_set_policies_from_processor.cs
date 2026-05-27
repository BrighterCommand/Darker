using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_query_context_has_no_policies_should_set_policies_from_processor
    {
        [Fact]
        public void Execute_without_policies_on_context_should_use_processor_registry()
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

            // A policy registry owned by the processor
            var processorRegistry = new PolicyRegistry();

            var queryProcessor = new QueryProcessor(
                handlerConfiguration,
                new InMemoryQueryContextFactory(),
                policyRegistry: processorRegistry);

            // Act — call Execute without providing a context
            queryProcessor.Execute(new SyncTestQuery(id), queryContext: null);

            // Assert — handler's Context.Policies is the same registry passed to the constructor
            handler.CapturedContext.Policies.ShouldBeSameAs(processorRegistry);
        }
    }
}
