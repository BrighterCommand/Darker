using System;
using Paramore.Darker.Builder;
using Paramore.Darker.Exceptions;
using Polly;
using Polly.Registry;

namespace Paramore.Darker.Policies
{
    public static class QueryProcessorBuilderExtensions
    {
        public static IBuildTheQueryProcessor Policies(this IBuildTheQueryProcessor builder, IPolicyRegistry<string> policyRegistry)
        {
            var queryProcessorBuilder = builder as QueryProcessorBuilder;
            if (queryProcessorBuilder == null)
                throw new NotSupportedException($"This extension method only supports the default {nameof(QueryProcessorBuilder)}.");

            AddPolicies(queryProcessorBuilder, policyRegistry);

            return queryProcessorBuilder;
        }

        public static TBuilder AddPolicies<TBuilder>(this TBuilder builder, IPolicyRegistry<string> policyRegistry)
            where TBuilder : IQueryProcessorExtensionBuilder
        {
            if (policyRegistry == null)
                throw new ArgumentNullException(nameof(policyRegistry));

            if (!policyRegistry.ContainsKey(Constants.RetryPolicyName))
                throw new ConfigurationException($"The policy registry is missing the {Constants.RetryPolicyName} policy which is required");

            if (!policyRegistry.ContainsKey(Constants.CircuitBreakerPolicyName))
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

        public static TBuilder AddDefaultPolicies<TBuilder>(this TBuilder builder)
            where TBuilder : IQueryProcessorExtensionBuilder
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

            var policyRegistry = new PolicyRegistry();
            policyRegistry.Add(Constants.RetryPolicyName, defaultRetryPolicy);
            policyRegistry.Add(Constants.CircuitBreakerPolicyName, circuitBreakerPolicy);

            builder.RegisterDecorator(typeof(RetryableQueryDecorator<,>));
            builder.AddContextBagItem(Constants.ContextBagKey, policyRegistry);

            return builder;
        }
    }
}