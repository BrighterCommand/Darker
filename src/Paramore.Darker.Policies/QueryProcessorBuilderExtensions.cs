using System;
using Paramore.Darker.Builder;
using Paramore.Darker.Exceptions;
using Polly;

namespace Paramore.Darker.Policies
{
    public static class QueryProcessorBuilderExtensions
    {
        public static IBuildTheQueryProcessor Policies(this IBuildTheQueryProcessor builder, IPolicyRegistry policyRegistry)
        {
            var queryProcessorBuilder = builder as QueryProcessorBuilder;
            if (queryProcessorBuilder == null)
                throw new NotSupportedException($"This extension method only supports the default {nameof(QueryProcessorBuilder)}.");

            AddPolicies(queryProcessorBuilder, policyRegistry);

            return queryProcessorBuilder;
        }
        
        public static IQueryProcessorExtensionBuilder AddPolicies(this IQueryProcessorExtensionBuilder builder, IPolicyRegistry policyRegistry)
        {
            if (policyRegistry == null)
                throw new ArgumentNullException(nameof(policyRegistry));

            if (!policyRegistry.Has(Constants.RetryPolicyName))
                throw new ConfigurationException($"The policy registry is missing the {Constants.RetryPolicyName} policy which is required");

            if (!policyRegistry.Has(Constants.CircuitBreakerPolicyName))
                throw new ConfigurationException($"The policy registry is missing the {Constants.CircuitBreakerPolicyName} policy which is required");

            builder.RegisterDecorator(typeof(RetryableQueryDecorator<,>));
            builder.AddContextBagItem(Constants.ContextBagKey, policyRegistry);

            return builder;
        }

        public static IBuildTheQueryProcessor DefaultPolicies(this IBuildTheQueryProcessor builder)
        {
            var queryProcessorBuilder = builder as QueryProcessorBuilder;
            if (queryProcessorBuilder == null)
                throw new NotSupportedException($"This extension method only supports the default {nameof(QueryProcessorBuilder)}.");

            AddDefaultPolicies(queryProcessorBuilder);

            return queryProcessorBuilder;
        }

        public static IQueryProcessorExtensionBuilder AddDefaultPolicies(this IQueryProcessorExtensionBuilder builder)
        {
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

            builder.RegisterDecorator(typeof(RetryableQueryDecorator<,>));
            builder.AddContextBagItem(Constants.ContextBagKey, policyRegistry);

            return builder;
        }
    }
}