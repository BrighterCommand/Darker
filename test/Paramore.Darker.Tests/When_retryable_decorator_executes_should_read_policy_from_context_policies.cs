using System;
using Paramore.Darker.Policies;
using Paramore.Darker.Policies.Handlers;
using Paramore.Darker.Tests.TestDoubles;
using Polly;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class When_retryable_decorator_executes_should_read_policy_from_context_policies
    {
        [Fact]
        public void Execute_with_policy_in_context_policies_should_execute_through_retry_policy()
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

            var policyRegistry = new PolicyRegistry
            {
                { Constants.RetryPolicyName, Policy.NoOp() }
            };

            // Pass policy registry via constructor — InitQueryContext will set Context.Policies
            // The Bag is intentionally empty to prove decorator reads from Context.Policies, not Context.Bag
            var queryProcessor = new QueryProcessor(
                handlerConfiguration,
                new InMemoryQueryContextFactory(),
                policyRegistry: policyRegistry);

            // Act
            var result = queryProcessor.Execute(new SyncTestQuery(id));

            // Assert
            result.ShouldNotBeNull();
            result.Value.ShouldBe(id);
        }
    }
}
