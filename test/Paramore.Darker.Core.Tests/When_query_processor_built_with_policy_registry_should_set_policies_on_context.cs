using System;
using Paramore.Darker.Builder;
using Paramore.Darker.Policies;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_query_processor_built_with_policy_registry_should_set_policies_on_context
    {
        [Fact]
        public void Build_with_default_policies_should_set_context_policies_on_execution()
        {
            // Arrange
            var id = Guid.NewGuid();
            var handler = new ContextCapturingHandler();

            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, ContextCapturingHandler>();

            var queryProcessor = QueryProcessorBuilder
                .With()
                .Handlers(
                    syncRegistry,
                    type => handler,
                    type => { },
                    type => null)
                .InMemoryQueryContextFactory()
                .DefaultPolicies()
                .Build();

            // Act
            queryProcessor.Execute(new SyncTestQuery(id));

            // Assert — policies set via builder should flow into Context.Policies
            handler.CapturedContext.Policies.ShouldNotBeNull();
            handler.CapturedContext.Policies.ContainsKey(Constants.RetryPolicyName).ShouldBeTrue();
            handler.CapturedContext.Policies.ContainsKey(Constants.CircuitBreakerPolicyName).ShouldBeTrue();
        }
    }
}
