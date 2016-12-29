using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Darker.Attributes;
using Darker.Exceptions;
using Darker.Logging;
using Darker.Serialization;

namespace Darker
{
    public sealed class QueryProcessor : IQueryProcessor
    {
        private static readonly ILog _logger = LogProvider.For<QueryProcessor>();

        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IRequestContextFactory _requestContextFactory;
        private readonly IQueryHandlerFactory _handlerFactory;
        private readonly IQueryHandlerDecoratorFactory _decoratorFactory;
        private readonly ISerializer _serializer;
        private readonly IReadOnlyDictionary<string, object> _contextBagData;

        public QueryProcessor(
            IHandlerConfiguration handlerConfiguration,
            IRequestContextFactory requestContextFactory,
            ISerializer serializer,
            IReadOnlyDictionary<string, object> contextBagData = null)
        {
            if (handlerConfiguration == null)
                throw new ArgumentNullException(nameof(handlerConfiguration));

            if (handlerConfiguration.HandlerRegistry == null)
                throw new ArgumentException($"{nameof(handlerConfiguration.HandlerRegistry)} must not be null", nameof(handlerConfiguration));
            if (handlerConfiguration.HandlerFactory == null)
                throw new ArgumentException($"{nameof(handlerConfiguration.HandlerFactory)} must not be null", nameof(handlerConfiguration));
            if (handlerConfiguration.DecoratorFactory == null)
                throw new ArgumentException($"{nameof(handlerConfiguration.DecoratorFactory)} must not be null", nameof(handlerConfiguration));

            _handlerRegistry = handlerConfiguration.HandlerRegistry;
            _handlerFactory = handlerConfiguration.HandlerFactory;
            _decoratorFactory = handlerConfiguration.DecoratorFactory;

            _requestContextFactory = requestContextFactory ?? throw new ArgumentNullException(nameof(requestContextFactory));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _contextBagData = contextBagData ?? new Dictionary<string, object>();
        }

        public TResponse Execute<TResponse>(IQueryRequest<TResponse> request)
            where TResponse : IQueryResponse
        {
            var requestType = request.GetType();
            _logger.InfoFormat("Building and executing pipeline for {RequestType}", requestType.Name);

            (var handlerType, var handler) = ResolveHandler(requestType);

            var requestContext = CreateRequestContext();
            handler.Context = requestContext;

            var decorators = GetDecorators<TResponse>(handlerType.GetMethod(nameof(IQueryHandler<IQueryRequest<TResponse>, TResponse>.Execute)), requestContext);

            _logger.Debug("Begin building pipeline...");

            var pipeline = new List<Func<IQueryRequest<TResponse>, TResponse>>
            {
                r => handler.Execute((dynamic)r)
            };

            // fallback doesn't have an incoming pipeline
            Func<IQueryRequest<TResponse>, TResponse> fallback = r => handler.Fallback((dynamic)r);

            foreach (var decorator in decorators)
            {
                _logger.DebugFormat("Adding decorator to pipeline: {Decorator}", decorator.GetType().Name);

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

        public async Task<TResponse> ExecuteAsync<TResponse>(IQueryRequest<TResponse> request, CancellationToken cancellationToken = default(CancellationToken))
            where TResponse : IQueryResponse
        {
            var requestType = request.GetType();
            _logger.InfoFormat("Building and executing async pipeline for {RequestType}", requestType.Name);

            (var handlerType, var handler) = ResolveHandler(requestType);

            var requestContext = CreateRequestContext();
            handler.Context = requestContext;

            var decorators = GetDecorators<TResponse>(handlerType.GetMethod(nameof(IQueryHandler<IQueryRequest<TResponse>, TResponse>.ExecuteAsync)), requestContext);

            _logger.Debug("Begin building async pipeline...");

            var pipeline = new List<Func<IQueryRequest<TResponse>, CancellationToken, Task<TResponse>>>
            {
                (r, ct) => handler.ExecuteAsync((dynamic)r, ct)
            };

            // fallback doesn't have an incoming pipeline
            Func<IQueryRequest<TResponse>, CancellationToken, Task<TResponse>> fallback = (r, ct) => handler.FallbackAsync((dynamic)r, ct);

            foreach (var decorator in decorators)
            {
                _logger.DebugFormat("Adding decorator to async pipeline: {Decorator}", decorator.GetType().Name);

                var next = pipeline.Last();
                pipeline.Add((r, ct) => decorator.ExecuteAsync(r, next, fallback, ct));
            }

            try
            {
                _logger.DebugFormat("Invoking async pipeline...");
                return await pipeline.Last().Invoke(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.InfoException("An exception was thrown during async pipeline execution", ex);
                throw;
            }
        }

        private (Type handlerType, dynamic handler) ResolveHandler(Type requestType)
        {
            _logger.DebugFormat("Looking up handler type in handler registry...");
            var handlerType = _handlerRegistry.Get(requestType);
            if (handlerType == null)
                throw new MissingHandlerException($"No handler registered for query: {requestType.FullName}");

            _logger.DebugFormat("Found handler type for {RequestType} in handler registry: {HandlerType}", requestType.Name, handlerType.Name);

            _logger.Debug("Resolving handler instance...");
            var handler = _handlerFactory.Create<dynamic>(handlerType);
            if (handler == null)
                throw new MissingHandlerException($"Handler could not be created for type: {handlerType.FullName}");

            _logger.Debug("Resolved handler instance");

            return (handlerType, handler);
        }

        private IRequestContext CreateRequestContext()
        {
            _logger.Debug("Creating request context...");

            var requestContext = _requestContextFactory.Create();
            requestContext.Serializer = _serializer;
            requestContext.Bag = _contextBagData.ToDictionary(d => d.Key, d => d.Value); // shallow copy

            return requestContext;
        }

        public IList<IQueryHandlerDecorator<IQueryRequest<TResponse>, TResponse>> GetDecorators<TResponse>(MethodInfo executeMethod, IRequestContext requestContext)
            where TResponse : IQueryResponse
        {
            var attributes = executeMethod.GetCustomAttributes(typeof(QueryHandlerAttribute), true)
                .Cast<QueryHandlerAttribute>()
                .OrderByDescending(attr => attr.Step)
                .ToList();

            _logger.DebugFormat("Found {AttributesCount} query handler attributes", attributes.Count);

            var decorators = new List<IQueryHandlerDecorator<IQueryRequest<TResponse>, TResponse>>();
            foreach (var attribute in attributes)
            {
                var decoratorType = attribute.GetDecoratorType().MakeGenericType(typeof(IQueryRequest<TResponse>), typeof(TResponse));

                _logger.DebugFormat("Resolving decorator instance of type {DecoratorType}...", decoratorType.Name);
                var decorator = _decoratorFactory.Create<IQueryHandlerDecorator<IQueryRequest<TResponse>, TResponse>>(decoratorType);
                if (decorator == null)
                    throw new MissingHandlerDecoratorException($"Handler decorator could not be created for type: {decoratorType.FullName}");

                decorator.Context = requestContext;

                _logger.DebugFormat("Initialising decorator from attribute params...");
                decorator.InitializeFromAttributeParams(attribute.GetAttributeParams());

                decorators.Add(decorator);
            }

            _logger.DebugFormat("Finished initialising {DecoratorsCount} decorators", decorators.Count);

            return decorators;
        }
    }
}