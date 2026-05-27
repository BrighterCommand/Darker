using System;
using System.Collections.Generic;
using Paramore.Darker.Core.Tests.TestDoubles;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_caller_provides_context_with_policies_should_preserve_caller_policies
    {
        [Fact]
        public void Execute_with_caller_context_policies_should_not_be_overwritten_by_processor_registry()
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

            // Processor has its own registry
            var processorRegistry = new PolicyRegistry();

            var queryProcessor = new QueryProcessor(
                handlerConfiguration,
                new InMemoryQueryContextFactory(),
                policyRegistry: processorRegistry);

            // Caller provides a context with a different policy registry
            var callerRegistry = new PolicyRegistry();
            var callerContext = new QueryContext
            {
                Bag = new Dictionary<string, object>(),
                Policies = callerRegistry
            };

            // Act — call Execute with caller-provided context that already has Policies set
            queryProcessor.Execute(new SyncTestQuery(id), queryContext: callerContext);

            // Assert — callerRegistry wins, processorRegistry is not used
            handler.CapturedContext.Policies.ShouldBeSameAs(callerRegistry);
            handler.CapturedContext.Policies.ShouldNotBeSameAs(processorRegistry);
        }
    }
}
