using System;
using Darker.Builder;
using Polly;
using Darker.Exceptions;

namespace Darker.Policies
{
    public static class QueryProcessorBuilderExtensions
    {
        public static IBuildTheQueryProcessor Policies(this IBuildTheQueryProcessor lastStageBuilder, IPolicyRegistry policyRegistry)
        {
            var builder = lastStageBuilder.ToQueryProcessorBuilder();

            if (policyRegistry == null)
                throw new ArgumentNullException(nameof(policyRegistry));

            if (!policyRegistry.Has(Constants.RetryPolicyName))
                throw new ConfigurationException($"The policy registry is missing the {Constants.RetryPolicyName} policy which is required");

            if (!policyRegistry.Has(Constants.CircuitBreakerPolicyName))
                throw new ConfigurationException($"The policy registry is missing the {Constants.CircuitBreakerPolicyName} policy which is required");

            return builder.ContextBagItem(Constants.ContextBagKey, policyRegistry);
        }

        public static IBuildTheQueryProcessor DefaultPolicies(this IBuildTheQueryProcessor lastStageBuilder)
        {
            var builder = lastStageBuilder.ToQueryProcessorBuilder();

            var defaultRetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetry(new[]
                {
                    TimeSpan.FromMilliseconds(50),
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(150)
                });

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreaker(1, TimeSpan.FromMilliseconds(500));

            var policyRegistry = new PolicyRegistry
            {
                { Constants.RetryPolicyName, defaultRetryPolicy },
                { Constants.CircuitBreakerPolicyName, circuitBreakerPolicy }
            };

            return builder.ContextBagItem(Constants.ContextBagKey, policyRegistry);
        }
    }
}