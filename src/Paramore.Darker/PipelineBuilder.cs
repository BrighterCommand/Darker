using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Attributes;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Logging;

namespace Paramore.Darker
{
    internal sealed class PipelineBuilder<TResult> : IDisposable
    {
        private const string ExecuteMethodName = nameof(IQueryHandler<IQuery<TResult>, TResult>.Execute);
        private const string ExecuteAsyncMethodName = nameof(IQueryHandler<IQuery<TResult>, TResult>.ExecuteAsync);

        private static readonly ILogger _logger = ApplicationLogging.CreateLogger<PipelineBuilder<TResult>>();

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

        public Func<IQuery<TResult>, TResult> Build(IQuery<TResult> query, IQueryContext queryContext)
        {
            var queryType = query.GetType();
            _logger.LogInformation("Building pipeline for {QueryType}", queryType.Name);

            var (handlerType, handler) = ResolveHandler(queryType);

            _handler = handler;
            _handler.Context = queryContext;

            var executeMethodInfo = GetExecuteMethodInfo(handlerType, queryType) as MethodInfo;
            _decorators = GetDecorators(executeMethodInfo, queryContext);

            var pipeline = new List<Func<IQuery<TResult>, TResult>>
            {
                r =>
                    {
                        try
                        {
                            return (TResult)executeMethodInfo.Invoke(_handler, new object[] { r });
                        }
                        catch (TargetInvocationException targetInvocationException)
                        {
                            // Unwrap the original exception instead of wrapping it in a FormatException
                            throw targetInvocationException.InnerException;
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
                pipeline.Add(r => decorator.Execute(r, next, fallback));
            }

            return pipeline.Last();
        }

        public Func<IQuery<TResult>, CancellationToken, Task<TResult>> BuildAsync(IQuery<TResult> query, IQueryContext queryContext)
        {
            var queryType = query.GetType();
            _logger.LogInformation("Building and executing async pipeline for {QueryType}", queryType.Name);

            var (handlerType, handler) = ResolveHandler(queryType);

            _handler = handler;
            _handler.Context = queryContext;

            var executeAsyncMethodInfo = GetExecuteAsyncMethodInfo(handlerType, queryType) as MethodInfo;
            _decorators = GetDecorators(executeAsyncMethodInfo, queryContext);

            var pipeline = new List<Func<IQuery<TResult>, CancellationToken, Task<TResult>>>
            {
                (r, ct) =>
                {
                    try
                    {
                        return (Task<TResult>)executeAsyncMethodInfo.Invoke(_handler, new object[] { r, ct });
                    }
                    catch (TargetInvocationException targetInvocationException)
                    {
                        // Unwrap the original exception instead of wrapping it in a FormatException
                        throw targetInvocationException.InnerException;
                    }
                }
            };

            // fallback doesn't have an incoming pipeline
            var fallbackMethodInfo = handlerType.GetMethod("FallbackAsync", new[] { queryType, typeof(CancellationToken) });
            Func<IQuery<TResult>, CancellationToken, Task<TResult>> fallback = (r, ct) => (Task<TResult>)fallbackMethodInfo.Invoke(_handler, new object[] { r, ct });

            foreach (var decorator in _decorators)
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

        private (Type handlerType, IQueryHandler handler) ResolveHandler(Type queryType)
        {
            _logger.LogDebug("Looking up handler type in handler registry...");
            var handlerType = _handlerRegistry.Get(queryType);
            if (handlerType == null)
                throw new MissingHandlerException($"No handler registered for query: {queryType.FullName}");

            _logger.LogDebug("Found handler type for {QueryType} in handler registry: {HandlerType}", queryType.Name, handlerType.Name);

            _logger.LogDebug("Resolving handler instance...");
            var handler = _handlerFactory.Create(handlerType);
            if (handler == null)
                throw new MissingHandlerException($"Handler could not be created for type: {handlerType.FullName}");

            return (handlerType, handler);
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
                var decorator = _decoratorFactory.Create<IQueryHandlerDecorator<IQuery<TResult>, TResult>>(decoratorType);
                if (decorator == null)
                    throw new MissingHandlerDecoratorException($"Handler decorator could not be created for type: {decoratorType.FullName}");

                decorator.Context = queryContext;

                _logger.LogDebug("Initialising decorator from attribute params...");
                decorator.InitializeFromAttributeParams(attribute.GetAttributeParams());

                decorators.Add(decorator);
            }

            _logger.LogDebug("Finished initialising {DecoratorsCount} decorators", decorators.Count);

            return decorators;
        }

        public void Dispose()
        {
            _logger.LogDebug("Disposing pipeline; releasing handler and decorators.");

            _handlerFactory?.Release(_handler);

            if (_decorators != null && _decorators.Any())
            {
                foreach (var decorator in _decorators)
                {
                    _decoratorFactory.Release(decorator);
                }
            }
        }
    }
}