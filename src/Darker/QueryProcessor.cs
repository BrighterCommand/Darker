using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Darker.Attributes;
using Darker.Exceptions;
using Darker.Logging;

#if NETSTANDARD1_0
using System.Reflection;
#endif

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
            // todo: c# 7 tuples to the rescue pls!
            var deconstructMe = ResolveHandler(request);
            var handlerType = deconstructMe.Item1;
            var handler = deconstructMe.Item2;

            var requestContext = CreateRequestContext();
            handler.Context = requestContext;

            var decorators = GetDecorators<TResponse>(handlerType.GetMethod(nameof(IQueryHandler<IQueryRequest<TResponse>, TResponse>.Execute)), requestContext);

            _logger.Debug("Begin building pipeline...");

            var pipeline = new List<Func<IQueryRequest<TResponse>, TResponse>>
            {
                r => handler.Execute((dynamic)r)
            };

            // fallback is doesn't have an incoming pipeline
            Func<IQueryRequest<TResponse>, TResponse> fallback = r => handler.Fallback((dynamic)r);

            foreach (var decorator in decorators)
            {
                _logger.DebugFormat("Adding decorator to pipeline: {decorator}", decorator.GetType().Name);

                var next = pipeline.Last();
                pipeline.Add(r => decorator.Execute(r, next, fallback));
            }

            try
            {
                _logger.DebugFormat("Invoking pipeline...");
                return pipeline.Last().Invoke(request);
            }
            catch (Exception ex)
            {
                _logger.InfoException("An exception was thrown during pipeline execution", ex);
                throw;
            }
        }

        public async Task<TResponse> ExecuteAsync<TResponse>(IQueryRequest<TResponse> request)
            where TResponse : IQueryResponse
        {
            // todo: c# 7 tuples to the rescue pls!
            var deconstructMe = ResolveHandler(request);
            var handlerType = deconstructMe.Item1;
            var handler = deconstructMe.Item2;

            var requestContext = CreateRequestContext();
            handler.Context = requestContext;

            var decorators = GetDecorators<TResponse>(handlerType.GetMethod(nameof(IQueryHandler<IQueryRequest<TResponse>, TResponse>.ExecuteAsync)), requestContext);

            _logger.Debug("Begin building pipeline...");

            var pipeline = new List<Func<IQueryRequest<TResponse>, Task<TResponse>>>
            {
                r => handler.ExecuteAsync((dynamic)r)
            };

            // fallback is doesn't have an incoming pipeline
            Func<IQueryRequest<TResponse>, Task<TResponse>> fallback = r => handler.FallbackAsync((dynamic)r);

            foreach (var decorator in decorators)
            {
                _logger.DebugFormat("Adding decorator to pipeline: {decorator}", decorator.GetType().Name);

                var next = pipeline.Last();
                pipeline.Add(r => decorator.ExecuteAsync(r, next, fallback));
            }

            try
            {
                _logger.DebugFormat("Invoking pipeline...");
                return await pipeline.Last().Invoke(request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.InfoException("An exception was thrown during pipeline execution", ex);
                throw;
            }
        }

        private Tuple<Type, dynamic> ResolveHandler<TResponse>(IQueryRequest<TResponse> request)
            where TResponse : IQueryResponse
        {
            var requestType = request.GetType();
            _logger.InfoFormat("Building and executing pipeline for {requestType}", requestType.Name);

            _logger.DebugFormat("Looking up handler type in handler registry...");
            var handlerType = _handlerRegistry.Get(requestType);
            if (handlerType == null)
                throw new MissingHandlerException($"No handler registered for query: {requestType.FullName}");

            _logger.DebugFormat("Found handler type for {requestType} in handler registry: {handlerType}", requestType.Name, handlerType.Name);

            _logger.Debug("Resolving handler instance...");
            var handler = _handlerFactory.Create<dynamic>(handlerType);
            if (handler == null)
                throw new MissingHandlerException($"Handler could not be created for type: {handlerType.FullName}");

            _logger.Debug("Resolved handler instance");

            return new Tuple<Type, dynamic>(handlerType, handler);
        }

        private IRequestContext CreateRequestContext()
        {
            _logger.Debug("Creating request context...");
            var requestContext = _requestContextFactory.Create();
            requestContext.Policies = _policyRegistry;
            requestContext.Bag = new Dictionary<string, object>();

            return requestContext;
        }

        public IList<IQueryHandlerDecorator<IQueryRequest<TResponse>, TResponse>> GetDecorators<TResponse>(MethodInfo executeMethod, IRequestContext requestContext)
            where TResponse : IQueryResponse
        {
            var attributes = executeMethod.GetCustomAttributes(typeof(QueryHandlerAttribute), true)
                .Cast<QueryHandlerAttribute>()
                .OrderByDescending(attr => attr.Step)
                .ToList();

            _logger.DebugFormat("Found {attributesCount} query handler attributes", attributes.Count);

            var decorators = new List<IQueryHandlerDecorator<IQueryRequest<TResponse>, TResponse>>();
            foreach (var attribute in attributes)
            {
                var decoratorType = attribute.GetDecoratorType().MakeGenericType(typeof(IQueryRequest<TResponse>), typeof(TResponse));

                _logger.DebugFormat("Resolving decorator instance of type {decoratorType}...", decoratorType.Name);
                var decorator = _decoratorFactory.Create<IQueryHandlerDecorator<IQueryRequest<TResponse>, TResponse>>(decoratorType);
                if (decorator == null)
                    throw new MissingHandlerException($"Handler decorator could not be created for type: {decoratorType.FullName}");

                decorator.Context = requestContext;

                _logger.DebugFormat("Initialising decorator from attribute params...");
                decorator.InitializeFromAttributeParams(attribute.GetAttributeParams());

                decorators.Add(decorator);
            }

            _logger.DebugFormat("Finished initialising {decoratorsCount} decorators", decorators.Count);

            return decorators;
        }
    }
}