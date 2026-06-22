using System;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Policies;
using Polly;
using Polly.Registry;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    public static class PolicyDIExtensions
    {
        public static IDarkerHandlerBuilder AddPolicies(this IDarkerHandlerBuilder builder, IPolicyRegistry<string> policyRegistry)
        {
            QueryProcessorBuilderExtensions.AddPolicies(builder, policyRegistry);
            builder.Services.AddSingleton<IPolicyRegistry<string>>(policyRegistry);
            return builder;
        }

        /// <summary>
        /// Registers the resilience pipeline decorators and registers the supplied registry as the
        /// <see cref="ResiliencePipelineProvider{TKey}"/> service so it is threaded onto the query context.
        /// </summary>
        /// <param name="builder">The Darker handler builder.</param>
        /// <param name="resiliencePipelineRegistry">The registry of named resilience pipelines; must not be null.</param>
        /// <returns>The builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resiliencePipelineRegistry"/> is null.</exception>
        public static IDarkerHandlerBuilder AddResiliencePipelines(this IDarkerHandlerBuilder builder, ResiliencePipelineRegistry<string> resiliencePipelineRegistry)
        {
            QueryProcessorBuilderExtensions.AddResiliencePipelines(builder, resiliencePipelineRegistry);
            builder.Services.AddSingleton<ResiliencePipelineProvider<string>>(resiliencePipelineRegistry);
            return builder;
        }

        /// <summary>
        /// Registers the resilience pipeline decorators with a built-in default retry and circuit-breaker
        /// pipeline under their well-known keys, and registers the provider service.
        /// </summary>
        /// <param name="builder">The Darker handler builder.</param>
        /// <returns>The builder, for chaining.</returns>
        public static IDarkerHandlerBuilder AddDefaultResiliencePipelines(this IDarkerHandlerBuilder builder)
        {
            return AddResiliencePipelines(builder, QueryProcessorBuilderExtensions.DefaultResiliencePipelineRegistry());
        }

        public static IDarkerHandlerBuilder AddDefaultPolicies(this IDarkerHandlerBuilder builder)
        {
            var defaultRetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromMilliseconds(50),
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(150)
                });

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(1, TimeSpan.FromMilliseconds(500));

            var policyRegistry = new PolicyRegistry
            {
                { Constants.RetryPolicyName, defaultRetryPolicy },
                { Constants.CircuitBreakerPolicyName, circuitBreakerPolicy }
            };

            return AddPolicies(builder, policyRegistry);
        }
    }
}
