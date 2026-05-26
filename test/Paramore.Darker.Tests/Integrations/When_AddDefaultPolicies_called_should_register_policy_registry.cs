using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Policies;
using Paramore.Darker.Tests.TestDoubles;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests.Integrations
{
    public class When_AddDefaultPolicies_called_should_register_policy_registry
    {
        [Fact]
        public void AddDefaultPolicies_should_register_IPolicyRegistry_in_DI()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddAsyncHandlers(r => r.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncRetryableQueryHandler>())
                .AddDefaultPolicies();
            services.AddTransient<AsyncRetryableQueryHandler>();

            var provider = services.BuildServiceProvider();

            // Act — resolve the registry that AddDefaultPolicies should have registered
            var policyRegistry = provider.GetService<IPolicyRegistry<string>>();

            // Assert — registry is registered and contains the default policies
            policyRegistry.ShouldNotBeNull();
            policyRegistry.ContainsKey(Constants.RetryPolicyName).ShouldBeTrue();
            policyRegistry.ContainsKey(Constants.CircuitBreakerPolicyName).ShouldBeTrue();
        }

        [Fact]
        public async Task AddDefaultPolicies_should_flow_policies_into_async_query_execution()
        {
            // Arrange
            var id = Guid.NewGuid();
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddAsyncHandlers(r => r.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncRetryableQueryHandler>())
                .AddDefaultPolicies();
            services.AddTransient<AsyncRetryableQueryHandler>();

            var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act — handler with [RetryableQueryAsync] requires Context.Policies to be set (default policies are async)
            var result = await queryProcessor.ExecuteAsync(new AsyncTestQuery(id));

            // Assert
            result.ShouldNotBeNull();
            result.Value.ShouldBe(id);
        }
    }
}
