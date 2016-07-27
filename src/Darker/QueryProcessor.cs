using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Darker.Attributes;
using Darker.Exceptions;
using Darker.Logging;

namespace Darker
{
    public sealed class QueryProcessor : IQueryProcessor
    {
        public const string RetryPolicyName = "Darker.RetryPolicy";
        public const string CircuitBreakerPolicyName = "Darker.CircuitBreakerPolicy";

        private static readonly ILog _logger = LogProvider.For<QueryProcessor>();

        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IPolicyRegistry _policyRegistry;
        private readonly IRequestContextFactory _requestContextFactory;
        private readonly IQueryHandlerFactory _handlerFactory;
        private readonly IQueryHandlerDecoratorFactory _decoratorFactory;

        public QueryProcessor(IHandlerConfiguration handlerConfiguration, IPolicyRegistry policyRegistry, IRequestContextFactory requestContextFactory)
        {
            if (handlerConfiguration == null)
                throw new ArgumentNullException(nameof(handlerConfiguration));
            if (policyRegistry == null)
                throw new ArgumentNullException(nameof(policyRegistry));
            if (requestContextFactory == null)
                throw new ArgumentNullException(nameof(requestContextFactory));

            if (handlerConfiguration.HandlerRegistry == null)
                throw new ArgumentException($"{nameof(handlerConfiguration.HandlerRegistry)} must not be null", nameof(handlerConfiguration));
            if (handlerConfiguration.HandlerFactory == null)
                throw new ArgumentException($"{nameof(handlerConfiguration.HandlerFactory)} must not be null", nameof(handlerConfiguration));
            if (handlerConfiguration.DecoratorFactory == null)
                throw new ArgumentException($"{nameof(handlerConfiguration.DecoratorFactory)} must not be null", nameof(handlerConfiguration));

            _handlerRegistry = handlerConfiguration.HandlerRegistry;
            _handlerFactory = handlerConfiguration.HandlerFactory;
            _decoratorFactory = handlerConfiguration.DecoratorFactory;
            _policyRegistry = policyRegistry;
            _requestContextFactory = requestContextFactory;
        }

        public TResponse Execute<TResponse>(IQueryRequest<TResponse> request)
            where TResponse : IQueryResponse
        {
            var requestType = request.GetType();
            _logger.InfoFormat("Building and executing pipeline for {0}", requestType.Name);

            _logger.DebugFormat("Looking up handler type in handler registry...");
            var handlerType = _handlerRegistry.Get(requestType);
            _logger.DebugFormat("Found handler type for {0} in handler registry: {1}", requestType.Name, handlerType.Name);

            _logger.Debug("Resolving handler instance...");
            var handler = _handlerFactory.Create<dynamic>(handlerType);
            _logger.Debug("Resolved handler instance");

            _logger.Debug("Creating request context...");
            var requestContext = _requestContextFactory.Create();
            requestContext.Policies = _policyRegistry;
            requestContext.Bag = new Dictionary<string, object>();

            handler.Context = requestContext;

            var attributes = handlerType.GetMethod(nameof(Execute))
                .GetCustomAttributes(typeof(QueryHandlerAttribute), true)
                .Cast<QueryHandlerAttribute>()
                .OrderByDescending(attr => attr.Step)
                .ToList();

            _logger.DebugFormat("Found {0} query handler attributes", attributes.Count);

            var decorators = new List<IQueryHandlerDecorator<IQueryRequest<TResponse>, TResponse>>();
            foreach (var attribute in attributes)
            {
                var decoratorType = attribute.GetDecoratorType().MakeGenericType(typeof(IQueryRequest<TResponse>), typeof(TResponse));

                _logger.DebugFormat("Resolving decorator instance of type {0}...", decoratorType.Name);
                var decorator = _decoratorFactory.Create<IQueryHandlerDecorator<IQueryRequest<TResponse>, TResponse>>(decoratorType);
                decorator.Context = requestContext;

                _logger.DebugFormat("Initialising decorator from attribute params...");
                decorator.InitializeFromAttributeParams(attribute.GetAttributeParams());

                decorators.Add(decorator);
            }

            _logger.DebugFormat("Finished initialising {0} decorators", decorators.Count);
            _logger.Debug("Begin building pipeline...");

            var pipeline = new List<Func<IQueryRequest<TResponse>, TResponse>>
            {
                r => handler.Execute((dynamic)r)
            };

            foreach (var decorator in decorators)
            {
                _logger.DebugFormat("Adding decorator to pipeline: {0}", decorator.GetType().Name);

                var next = pipeline.Last();
                pipeline.Add(r => decorator.Execute(r, next));
            }

            try
            {
                _logger.DebugFormat("Invoking pipeline...");
                return pipeline.Last().Invoke(request);
            }
            catch (FallbackException)
            {
                return handler.Fallback((dynamic)request);
            }
            catch (Exception ex)
            {
                _logger.InfoException("An exception was thrown during pipeline execution", ex);
                throw;
            }
        }
    }
}