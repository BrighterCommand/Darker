using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Logging;
using Paramore.Darker.Observability;
using System.Runtime.ExceptionServices;

namespace Paramore.Darker
{
    internal sealed class PipelineBuilder<TResult> : IDisposable
    {
        private const string ExecuteMethodName = nameof(IQueryHandler<IQuery<TResult>, TResult>.Execute);
        private const string ExecuteAsyncMethodName = nameof(IQueryHandlerAsync<IQuery<TResult>, TResult>.ExecuteAsync);

        private static readonly ILogger _logger = ApplicationLogging.CreateLogger<PipelineBuilder<TResult>>();

        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryHandlerFactory _handlerFactory;
        private readonly IQueryHandlerDecoratorFactory _decoratorFactory;

        private readonly IQueryHandlerRegistryAsync _handlerRegistryAsync;
        private readonly IQueryHandlerFactoryAsync _handlerFactoryAsync;
        private readonly IQueryHandlerDecoratorFactoryAsync _decoratorFactoryAsync;

        private IQueryHandler _handler;
        private IReadOnlyList<IQueryHandlerDecorator<IQuery<TResult>, TResult>> _decorators;
        private IReadOnlyList<IQueryHandlerDecoratorAsync<IQuery<TResult>, TResult>> _asyncDecorators;
        private IAmALifetime _lifetime;

        public PipelineBuilder(
            IQueryHandlerRegistry handlerRegistry,
            IQueryHandlerFactory handlerFactory,
            IQueryHandlerDecoratorFactory decoratorFactory,
            IQueryHandlerRegistryAsync handlerRegistryAsync = null,
            IQueryHandlerFactoryAsync handlerFactoryAsync = null,
            IQueryHandlerDecoratorFactoryAsync decoratorFactoryAsync = null)
        {
            _handlerRegistry = handlerRegistry;
            _handlerFactory = handlerFactory;
            _decoratorFactory = decoratorFactory;
            _handlerRegistryAsync = handlerRegistryAsync;
            _handlerFactoryAsync = handlerFactoryAsync;
            _decoratorFactoryAsync = decoratorFactoryAsync;
        }

        public Func<IQuery<TResult>, TResult> Build(IQuery<TResult> query, IQueryContext queryContext,
            InstrumentationOptions instrumentationOptions = InstrumentationOptions.None)
        {
            // Create the per-query lifetime before any Create call so a partial build still has an
            // owner for any resources (e.g. a child service scope) attached during resolution.
            _lifetime = new QueryLifetimeScope();

            var queryType = query.GetType();
            _logger.LogInformation("Building pipeline for {QueryType}", queryType.Name);

            var (handlerType, handler) = ResolveHandler(queryType);

            _handler = handler;
            _handler.Context = queryContext;

            var executeMethodInfo = GetExecuteMethodInfo(handlerType, queryType) as MethodInfo;
            ValidateNoMismatchedAttributes(executeMethodInfo, typeof(QueryHandlerAttributeAsync),
                "Sync handler has async attribute(s) on Execute. Use sync attributes (e.g. QueryHandlerAttribute) for sync handlers, or switch to an async handler with ExecuteAsync.");
            _decorators = GetDecorators(executeMethodInfo, queryContext);

            // Capture the span once; null when no tracer is configured (WriteQueryEvent is null-safe).
            var span = queryContext.Span;

            var pipeline = new List<Func<IQuery<TResult>, TResult>>
            {
                r =>
                    {
                        DarkerTracer.WriteQueryEvent(span, handlerType.Name, isAsync: false, instrumentationOptions, isSink: true);
                        try
                        {
                            return (TResult)executeMethodInfo.Invoke(_handler, new object[] { r });
                        }
                        catch (TargetInvocationException ex) when (ex.InnerException != null)
                        {
                            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                            throw; // never reached
                        }
                    }
            };

            // fallback doesn't have an incoming pipeline
            var fallbackMethodInfo = handlerType.GetMethod("Fallback", new[] { queryType });
            Func<IQuery<TResult>, TResult> fallback = r => (TResult)fallbackMethodInfo.Invoke(_handler, new object[] { r });

            foreach (var decorator in _decorators)
            {
                _logger.LogDebug("Adding decorator to pipeline: {Decorator}", decorator.GetType().Name);

                var next = pipeline.Last();
                pipeline.Add(r =>
                {
                    DarkerTracer.WriteQueryEvent(span, decorator.GetType().Name, isAsync: false, instrumentationOptions);
                    return decorator.Execute(r, next, fallback);
                });
            }

            return pipeline.Last();
        }

        public Func<IQuery<TResult>, CancellationToken, Task<TResult>> BuildAsync(IQuery<TResult> query, IQueryContext queryContext)
        {
            // Create the per-query lifetime before any Create call so a partial build still has an
            // owner for any resources (e.g. a child service scope) attached during resolution.
            _lifetime = new QueryLifetimeScope();

            var queryType = query.GetType();
            _logger.LogInformation("Building and executing async pipeline for {QueryType}", queryType.Name);

            var (handlerType, handler) = ResolveHandlerAsync(queryType);

            _handler = handler;
            _handler.Context = queryContext;

            var executeAsyncMethodInfo = GetExecuteAsyncMethodInfo(handlerType, queryType);
            if (executeAsyncMethodInfo == null)
                throw new ConfigurationException($"Handler {handlerType.FullName} does not implement ExecuteAsync. Register an async handler or use Execute instead.");

            ValidateNoMismatchedAttributes(executeAsyncMethodInfo, typeof(QueryHandlerAttribute),
                "Async handler has sync attribute(s) on ExecuteAsync. Use async attributes (e.g. QueryHandlerAttributeAsync) for async handlers, or switch to a sync handler with Execute.");

            _asyncDecorators = GetDecoratorsAsync(executeAsyncMethodInfo, queryContext);

            var pipeline = new List<Func<IQuery<TResult>, CancellationToken, Task<TResult>>>
            {
                (r, ct) =>
                {
                    try
                    {
                        return (Task<TResult>)executeAsyncMethodInfo.Invoke(_handler, new object[] { r, ct });
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        throw; // never reached for complier
                    }
                }
            };

            // fallback doesn't have an incoming pipeline
            var fallbackMethodInfo = handlerType.GetMethod("FallbackAsync", new[] { queryType, typeof(CancellationToken) });
            Func<IQuery<TResult>, CancellationToken, Task<TResult>> fallback = (r, ct) => (Task<TResult>)fallbackMethodInfo.Invoke(_handler, new object[] { r, ct });

            foreach (var decorator in _asyncDecorators)
            {
                _logger.LogDebug("Adding decorator to async pipeline: {Decorator}", decorator.GetType().Name);

                var next = pipeline.Last();
                pipeline.Add((r, ct) => decorator.ExecuteAsync(r, next, fallback, ct));
            }

            return pipeline.Last();
        }

        private static MemberInfo GetExecuteMethodInfo(Type handlerType, Type queryType)
        {
            return handlerType.GetMethod(ExecuteMethodName);
        }

        private static MethodInfo GetExecuteAsyncMethodInfo(Type handlerType, Type queryType)
        {
            return handlerType.GetMethod(ExecuteAsyncMethodName);
        }

        private static void ValidateNoMismatchedAttributes(MemberInfo methodInfo, Type wrongAttributeType, string message)
        {
            var mismatchedAttributes = methodInfo.GetCustomAttributes(wrongAttributeType, true);
            if (mismatchedAttributes.Length > 0)
                throw new ConfigurationException(message);
        }

        private (Type handlerType, IQueryHandler handler) ResolveHandler(Type queryType)
        {
            _logger.LogDebug("Looking up handler type in handler registry...");
            var handlerType = _handlerRegistry.Get(queryType);
            if (handlerType == null)
                throw new ConfigurationException($"No sync handler registered for query: {queryType.FullName}. If you have an async handler, use ExecuteAsync instead.");

            _logger.LogDebug("Found handler type for {QueryType} in handler registry: {HandlerType}", queryType.Name, handlerType.Name);

            _logger.LogDebug("Resolving handler instance...");
            var handler = _handlerFactory.Create(handlerType, _lifetime);
            if (handler == null)
                throw new ConfigurationException($"Handler could not be created for type: {handlerType.FullName}");

            return (handlerType, handler);
        }

        private (Type handlerType, IQueryHandler handler) ResolveHandlerAsync(Type queryType)
        {
            if (_handlerRegistryAsync != null && _handlerFactoryAsync != null)
            {
                _logger.LogDebug("Looking up handler type in async handler registry...");
                var handlerType = _handlerRegistryAsync.Get(queryType);
                if (handlerType == null)
                    throw new ConfigurationException($"No async handler registered for query: {queryType.FullName}. If you have a sync handler, use Execute instead.");

                _logger.LogDebug("Found handler type for {QueryType} in async handler registry: {HandlerType}", queryType.Name, handlerType.Name);

                _logger.LogDebug("Resolving async handler instance...");
                var handler = _handlerFactoryAsync.Create(handlerType, _lifetime);
                if (handler == null)
                    throw new ConfigurationException($"Async handler could not be created for type: {handlerType.FullName}");

                return (handlerType, handler);
            }

            // Fallback to sync registry for backwards compatibility during structural migration
            return ResolveHandler(queryType);
        }

        private IReadOnlyList<IQueryHandlerDecorator<IQuery<TResult>, TResult>> GetDecorators(MemberInfo executeMethod, IQueryContext queryContext)
        {
            var attributes = executeMethod.GetCustomAttributes(typeof(QueryHandlerAttribute), true)
                .Cast<QueryHandlerAttribute>()
                .OrderByDescending(attr => attr.Step)
                .ToList();

            _logger.LogDebug("Found {AttributesCount} query handler attributes", attributes.Count);

            var decorators = new List<IQueryHandlerDecorator<IQuery<TResult>, TResult>>();
            foreach (var attribute in attributes)
            {
                var decoratorType = attribute.GetDecoratorType().MakeGenericType(typeof(IQuery<TResult>), typeof(TResult));

                _logger.LogDebug("Resolving decorator instance of type {DecoratorType}...", decoratorType.Name);
                var decorator = _decoratorFactory.Create<IQueryHandlerDecorator<IQuery<TResult>, TResult>>(decoratorType, _lifetime);
                if (decorator == null)
                    throw new ConfigurationException($"Decorator could not be created for type: {decoratorType.FullName}. Ensure it is registered in the decorator registry.");

                decorator.Context = queryContext;

                _logger.LogDebug("Initialising decorator from attribute params...");
                decorator.InitializeFromAttributeParams(attribute.GetAttributeParams());

                decorators.Add(decorator);
            }

            _logger.LogDebug("Finished initialising {DecoratorsCount} decorators", decorators.Count);

            return decorators;
        }

        private IReadOnlyList<IQueryHandlerDecoratorAsync<IQuery<TResult>, TResult>> GetDecoratorsAsync(MemberInfo executeMethod, IQueryContext queryContext)
        {
            var attributes = executeMethod.GetCustomAttributes(typeof(QueryHandlerAttributeAsync), true)
                .Cast<QueryHandlerAttributeAsync>()
                .OrderByDescending(attr => attr.Step)
                .ToList();

            _logger.LogDebug("Found {AttributesCount} async query handler attributes", attributes.Count);

            var decorators = new List<IQueryHandlerDecoratorAsync<IQuery<TResult>, TResult>>();

            if (_decoratorFactoryAsync == null)
                return decorators;

            foreach (var attribute in attributes)
            {
                var decoratorType = attribute.GetDecoratorType().MakeGenericType(typeof(IQuery<TResult>), typeof(TResult));

                _logger.LogDebug("Resolving async decorator instance of type {DecoratorType}...", decoratorType.Name);
                var decorator = _decoratorFactoryAsync.Create<IQueryHandlerDecoratorAsync<IQuery<TResult>, TResult>>(decoratorType, _lifetime);
                if (decorator == null)
                    throw new ConfigurationException($"Decorator could not be created for type: {decoratorType.FullName}. Ensure it is registered in the decorator registry.");

                decorator.Context = queryContext;

                _logger.LogDebug("Initialising async decorator from attribute params...");
                decorator.InitializeFromAttributeParams(attribute.GetAttributeParams());

                decorators.Add(decorator);
            }

            _logger.LogDebug("Finished initialising {DecoratorsCount} async decorators", decorators.Count);

            return decorators;
        }

        public void Dispose()
        {
            _logger.LogDebug("Disposing pipeline; releasing handler and decorators.");

            _handlerFactory?.Release(_handler, _lifetime);

            if (_decorators != null && _decorators.Any())
            {
                foreach (var decorator in _decorators)
                {
                    _decoratorFactory.Release(decorator, _lifetime);
                }
            }

            if (_asyncDecorators != null && _asyncDecorators.Any())
            {
                foreach (var decorator in _asyncDecorators)
                {
                    _decoratorFactoryAsync?.Release(decorator, _lifetime);
                }
            }

            // Dispose the per-query lifetime last, after releasing the handler and decorators, so
            // any resources it owns (e.g. a child service scope) are torn down once per query.
            _lifetime?.Dispose();
        }
    }
}
