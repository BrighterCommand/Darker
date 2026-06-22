using System;
using Paramore.Darker.Builder;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Policies.Handlers;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Retry;

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
            queryProcessorBuilder.PolicyRegistry = policyRegistry;

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
            builder.RegisterDecorator(typeof(RetryableQueryDecoratorAsync<,>));

            return builder;
        }

        public static IBuildTheQueryProcessor ResiliencePipelines(this IBuildTheQueryProcessor builder, ResiliencePipelineRegistry<string> resiliencePipelineRegistry)
        {
            var queryProcessorBuilder = builder as QueryProcessorBuilder;
            if (queryProcessorBuilder == null)
                throw new NotSupportedException($"This extension method only supports the default {nameof(QueryProcessorBuilder)}.");

            AddResiliencePipelines(queryProcessorBuilder, resiliencePipelineRegistry);
            queryProcessorBuilder.ResiliencePipelineRegistry = resiliencePipelineRegistry;

            return queryProcessorBuilder;
        }

        public static TBuilder AddResiliencePipelines<TBuilder>(this TBuilder builder, ResiliencePipelineRegistry<string> resiliencePipelineRegistry)
            where TBuilder : IQueryProcessorExtensionBuilder
        {
            if (resiliencePipelineRegistry == null)
                throw new ArgumentNullException(nameof(resiliencePipelineRegistry));

            builder.RegisterDecorator(typeof(UseResiliencePipelineHandler<,>));
            builder.RegisterDecorator(typeof(UseResiliencePipelineHandlerAsync<,>));

            return builder;
        }

        public static IBuildTheQueryProcessor DefaultResiliencePipelines(this IBuildTheQueryProcessor builder)
        {
            return ResiliencePipelines(builder, DefaultResiliencePipelineRegistry());
        }

        public static TBuilder AddDefaultResiliencePipelines<TBuilder>(this TBuilder builder)
            where TBuilder : IQueryProcessorExtensionBuilder
        {
            return AddResiliencePipelines(builder, DefaultResiliencePipelineRegistry());
        }

        /// <summary>
        /// Builds a <see cref="ResiliencePipelineRegistry{TKey}"/> seeded with a default retry pipeline
        /// under <see cref="Constants.RetryPipelineName"/> and a default circuit-breaker pipeline under
        /// <see cref="Constants.CircuitBreakerPipelineName"/>. The builders are registered with the
        /// non-generic <c>TryAddBuilder</c>, so the defaults are shared-only and do not support
        /// <c>useTypePipeline</c>.
        /// </summary>
        /// <returns>A registry containing the default resilience pipelines.</returns>
        public static ResiliencePipelineRegistry<string> DefaultResiliencePipelineRegistry()
        {
            var registry = new ResiliencePipelineRegistry<string>();

            registry.TryAddBuilder(Constants.RetryPipelineName, (builder, _) =>
                builder.AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(50),
                    BackoffType = DelayBackoffType.Exponential
                }));

            registry.TryAddBuilder(Constants.CircuitBreakerPipelineName, (builder, _) =>
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = 2,
                    BreakDuration = TimeSpan.FromMilliseconds(500)
                }));

            return registry;
        }

        public static IBuildTheQueryProcessor DefaultPolicies(this IBuildTheQueryProcessor builder)
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

            return Policies(builder, policyRegistry);
        }

        public static TBuilder AddDefaultPolicies<TBuilder>(this TBuilder builder)
            where TBuilder : IQueryProcessorExtensionBuilder
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

            builder.RegisterDecorator(typeof(RetryableQueryDecorator<,>));
            builder.RegisterDecorator(typeof(RetryableQueryDecoratorAsync<,>));

            return builder;
        }
    }
}
