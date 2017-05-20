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
    internal sealed class PipelineBuilder<TResult> : IDisposable
    {
        private static readonly ILog _logger = LogProvider.For<PipelineBuilder<TResult>>();

        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryHandlerFactory _handlerFactory;
        private readonly IQueryHandlerDecoratorFactory _decoratorFactory;

        private IQueryHandler _handler;
        private IReadOnlyList<IQueryHandlerDecorator<IQuery<TResult>, TResult>> _decorators;

        public PipelineBuilder(IQueryHandlerRegistry handlerRegistry, IQueryHandlerFactory handlerFactory, IQueryHandlerDecoratorFactory decoratorFactory)
        {
            _handlerRegistry = handlerRegistry;
            _handlerFactory = handlerFactory;
            _decoratorFactory = decoratorFactory;
        }

        public IReadOnlyList<Func<IQuery<TResult>, TResult>> Build(IQuery<TResult> query, IQueryContext queryContext)
        {
            var queryType = query.GetType();
            _logger.InfoFormat("Building pipeline for {QueryType}", queryType.Name);

            (var handlerType, var handler) = ResolveHandler(queryType);

            _handler = handler;
            _handler.Context = queryContext;

            _decorators = GetDecorators(handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.Execute)), queryContext);

            var pipeline = new List<Func<IQuery<TResult>, TResult>>
            {
                r => ((dynamic)_handler).Execute((dynamic)r)
            };

            // fallback doesn't have an incoming pipeline
            Func<IQuery<TResult>, TResult> fallback = r => ((dynamic)_handler).Fallback((dynamic)r);

            foreach (var decorator in _decorators)
            {
                _logger.DebugFormat("Adding decorator to pipeline: {Decorator}", decorator.GetType().Name);

                var next = pipeline.Last();
                pipeline.Add(r => decorator.Execute(r, next, fallback));
            }

            return pipeline;
        }

        public IReadOnlyList<Func<IQuery<TResult>, CancellationToken, Task<TResult>>> BuildAsync(IQuery<TResult> query, IQueryContext queryContext)
        {
            var queryType = query.GetType();
            _logger.InfoFormat("Building and executing async pipeline for {QueryType}", queryType.Name);

            (var handlerType, var handler) = ResolveHandler(queryType);

            _handler = handler;
            _handler.Context = queryContext;

            _decorators = GetDecorators(handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.ExecuteAsync)), queryContext);

            var pipeline = new List<Func<IQuery<TResult>, CancellationToken, Task<TResult>>>
            {
                (r, ct) => ((dynamic)_handler).ExecuteAsync((dynamic)r, ct)
            };

            // fallback doesn't have an incoming pipeline
            Func<IQuery<TResult>, CancellationToken, Task<TResult>> fallback = (r, ct) => ((dynamic)_handler).FallbackAsync((dynamic)r, ct);

            foreach (var decorator in _decorators)
            {
                _logger.DebugFormat("Adding decorator to async pipeline: {Decorator}", decorator.GetType().Name);

                var next = pipeline.Last();
                pipeline.Add((r, ct) => decorator.ExecuteAsync(r, next, fallback, ct));
            }

            return pipeline;
        }

        private (Type handlerType, IQueryHandler handler) ResolveHandler(Type queryType)
        {
            _logger.DebugFormat("Looking up handler type in handler registry...");
            var handlerType = _handlerRegistry.Get(queryType);
            if (handlerType == null)
                throw new MissingHandlerException($"No handler registered for query: {queryType.FullName}");

            _logger.DebugFormat("Found handler type for {QueryType} in handler registry: {HandlerType}", queryType.Name, handlerType.Name);

            _logger.Debug("Resolving handler instance...");
            var handler = _handlerFactory.Create(handlerType);
            if (handler == null)
                throw new MissingHandlerException($"Handler could not be created for type: {handlerType.FullName}");

            return (handlerType, handler);
        }

        private IReadOnlyList<IQueryHandlerDecorator<IQuery<TResult>, TResult>> GetDecorators(MethodInfo executeMethod, IQueryContext queryContext)
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

        public void Dispose()
        {
            _logger.DebugFormat("Disposing pipeline; releasing handler and decorators.");

            _handlerFactory.Release(_handler);

            foreach (var decorator in _decorators)
            {
                _decoratorFactory.Release(decorator);
            }
        }
    }
}