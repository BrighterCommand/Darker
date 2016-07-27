using System;
using Darker.Exceptions;
using Polly;

namespace Darker.Builder
{
    public sealed class QueryProcessorBuilder : INeedHandlers, INeedPolicies, INeedARequestContext, IBuildTheQueryProcessor
    {
        private IHandlerConfiguration _handlerConfiguration;
        private IPolicyRegistry _policyRegistry;
        private IRequestContextFactory _requestContextFactory;

        public static INeedHandlers With()
        {
            return new QueryProcessorBuilder();
        }

        public INeedPolicies Handlers(IHandlerConfiguration handlerConfiguration)
        {
            if (handlerConfiguration == null)
                throw new ArgumentNullException(nameof(handlerConfiguration));

            _handlerConfiguration = handlerConfiguration;
            return this;
        }

        public INeedPolicies Handlers(IQueryHandlerRegistry handlerRegistry, IQueryHandlerFactory handlerFactory, IQueryHandlerDecoratorFactory decoratorFactory)
        {
            if (handlerRegistry == null)
                throw new ArgumentNullException(nameof(handlerRegistry));
            if (handlerFactory == null)
                throw new ArgumentNullException(nameof(handlerFactory));
            if (decoratorFactory == null)
                throw new ArgumentNullException(nameof(decoratorFactory));

            _handlerConfiguration = new HandlerConfiguration(handlerRegistry, handlerFactory, decoratorFactory);
            return this;
        }

        public INeedARequestContext Policies(IPolicyRegistry policyRegistry)
        {
            if (policyRegistry == null)
                throw new ArgumentNullException(nameof(policyRegistry));

            if (!policyRegistry.Has(QueryProcessor.RetryPolicyName))
                throw new ConfigurationException($"The policy registry is missing the {QueryProcessor.RetryPolicyName} policy which is required");

            if (!policyRegistry.Has(QueryProcessor.CircuitBreakerPolicyName))
                throw new ConfigurationException($"The policy registry is missing the {QueryProcessor.CircuitBreakerPolicyName} policy which is required");

            _policyRegistry = policyRegistry;
            return this;
        }

        public INeedARequestContext DefaultPolicies()
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

            _policyRegistry = new PolicyRegistry
            {
                { QueryProcessor.RetryPolicyName, defaultRetryPolicy },
                { QueryProcessor.CircuitBreakerPolicyName, circuitBreakerPolicy }
            };

            return this;
        }

        public IBuildTheQueryProcessor RequestContextFactory(IRequestContextFactory requestContextFactory)
        {
            if (requestContextFactory == null)
                throw new ArgumentNullException(nameof(requestContextFactory));

            _requestContextFactory = requestContextFactory;
            return this;
        }

        public IBuildTheQueryProcessor InMemoryRequestContextFactory()
        {
            _requestContextFactory = new InMemoryRequestContextFactory();
            return this;
        }

        public IQueryProcessor Build()
        {
            return new QueryProcessor(_handlerConfiguration, _policyRegistry, _requestContextFactory);
        }
    }
}