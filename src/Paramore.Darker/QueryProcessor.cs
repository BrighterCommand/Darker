using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Attributes;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Logging;

namespace Paramore.Darker
{
    public sealed class QueryProcessor : IQueryProcessor
    {
        private static readonly ILog _logger = LogProvider.For<QueryProcessor>();

        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryContextFactory _queryContextFactory;
        private readonly IQueryHandlerFactory _handlerFactory;
        private readonly IQueryHandlerDecoratorFactory _decoratorFactory;
        private readonly IReadOnlyDictionary<string, object> _contextBagData;

        public QueryProcessor(
            IHandlerConfiguration handlerConfiguration,
            IQueryContextFactory queryContextFactory,
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

            _queryContextFactory = queryContextFactory ?? throw new ArgumentNullException(nameof(queryContextFactory));
            _contextBagData = contextBagData ?? new Dictionary<string, object>();
        }

        public TResult Execute<TResult>(IQuery<TResult> query)
        {
            var queryType = query.GetType();
            _logger.InfoFormat("Building and executing pipeline for {QueryType}", queryType.Name);

            (var handlerType, var handler) = ResolveHandler(queryType);

            var queryContext = CreateQueryContext();
            handler.Context = queryContext;

            var decorators = GetDecorators<TResult>(handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.Execute)), queryContext);

            _logger.Debug("Begin building pipeline...");

            var pipeline = new List<Func<IQuery<TResult>, TResult>>
            {
                r => handler.Execute((dynamic)r)
            };

            // fallback doesn't have an incoming pipeline
            Func<IQuery<TResult>, TResult> fallback = r => handler.Fallback((dynamic)r);

            foreach (var decorator in decorators)
            {
                _logger.DebugFormat("Adding decorator to pipeline: {Decorator}", decorator.GetType().Name);

                var next = pipeline.Last();
                pipeline.Add(r => decorator.Execute(r, next, fallback));
            }

            try
            {
                _logger.DebugFormat("Invoking pipeline...");
                return pipeline.Last().Invoke(query);
            }
            catch (Exception ex)
            {
                _logger.InfoException("An exception was thrown during pipeline execution", ex);
                throw;
            }
        }

        public async Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default(CancellationToken))
        {
            var queryType = query.GetType();
            _logger.InfoFormat("Building and executing async pipeline for {QueryType}", queryType.Name);

            (var handlerType, var handler) = ResolveHandler(queryType);

            var queryContext = CreateQueryContext();
            handler.Context = queryContext;

            var decorators = GetDecorators<TResult>(handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.ExecuteAsync)), queryContext);

            _logger.Debug("Begin building async pipeline...");

            var pipeline = new List<Func<IQuery<TResult>, CancellationToken, Task<TResult>>>
            {
                (r, ct) => handler.ExecuteAsync((dynamic)r, ct)
            };

            // fallback doesn't have an incoming pipeline
            Func<IQuery<TResult>, CancellationToken, Task<TResult>> fallback = (r, ct) => handler.FallbackAsync((dynamic)r, ct);

            foreach (var decorator in decorators)
            {
                _logger.DebugFormat("Adding decorator to async pipeline: {Decorator}", decorator.GetType().Name);

                var next = pipeline.Last();
                pipeline.Add((r, ct) => decorator.ExecuteAsync(r, next, fallback, ct));
            }

            try
            {
                _logger.DebugFormat("Invoking async pipeline...");
                return await pipeline.Last().Invoke(query, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.InfoException("An exception was thrown during async pipeline execution", ex);
                throw;
            }
        }

        private (Type handlerType, dynamic handler) ResolveHandler(Type queryType)
        {
            _logger.DebugFormat("Looking up handler type in handler registry...");
            var handlerType = _handlerRegistry.Get(queryType);
            if (handlerType == null)
                throw new MissingHandlerException($"No handler registered for query: {queryType.FullName}");

            _logger.DebugFormat("Found handler type for {QueryType} in handler registry: {HandlerType}", queryType.Name, handlerType.Name);

            _logger.Debug("Resolving handler instance...");
            var handler = _handlerFactory.Create<dynamic>(handlerType);
            if (handler == null)
                throw new MissingHandlerException($"Handler could not be created for type: {handlerType.FullName}");

            _logger.Debug("Resolved handler instance");

            return (handlerType, handler);
        }

        private IQueryContext CreateQueryContext()
        {
            _logger.Debug("Creating query context...");

            var queryContext = _queryContextFactory.Create();

            // todo: no need for IQueryContext i think. just use dictionary
            queryContext.Bag = _contextBagData.ToDictionary(d => d.Key, d => d.Value); // shallow copy

            return queryContext;
        }

        public IList<IQueryHandlerDecorator<IQuery<TResult>, TResult>> GetDecorators<TResult>(MethodInfo executeMethod, IQueryContext queryContext)
        {
            var attributes = executeMethod.GetCustomAttributes(typeof(QueryHandlerAttribute), true)
                .Cast<QueryHandlerAttribute>()
                .OrderByDescending(attr => attr.Step)
                .ToList();

            _logger.DebugFormat("Found {AttributesCount} query handler attributes", attributes.Count);

            var decorators = new List<IQueryHandlerDecorator<IQuery<TResult>, TResult>>();
            foreach (var attribute in attributes)
            {
                var decoratorType = attribute.GetDecoratorType().MakeGenericType(typeof(IQuery<TResult>), typeof(TResult));

                _logger.DebugFormat("Resolving decorator instance of type {DecoratorType}...", decoratorType.Name);
                var decorator = _decoratorFactory.Create<IQueryHandlerDecorator<IQuery<TResult>, TResult>>(decoratorType);
                if (decorator == null)
                    throw new MissingHandlerDecoratorException($"Handler decorator could not be created for type: {decoratorType.FullName}");

                decorator.Context = queryContext;

                _logger.DebugFormat("Initialising decorator from attribute params...");
                decorator.InitializeFromAttributeParams(attribute.GetAttributeParams());

                decorators.Add(decorator);
            }

            _logger.DebugFormat("Finished initialising {DecoratorsCount} decorators", decorators.Count);

            return decorators;
        }
    }
}