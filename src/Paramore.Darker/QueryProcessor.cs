using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Logging;
using Paramore.Darker.Observability;
using Polly.Registry;
using System.Runtime.ExceptionServices;

namespace Paramore.Darker
{
    public sealed class QueryProcessor : IQueryProcessor
    {
        private static readonly ILogger _logger = ApplicationLogging.CreateLogger<QueryProcessor>();

        private readonly IQueryHandlerRegistry _handlerRegistry;
        private readonly IQueryContextFactory _queryContextFactory;
        private readonly IQueryHandlerFactory _handlerFactory;
        private readonly IQueryHandlerDecoratorFactory _decoratorFactory;

        private readonly IQueryHandlerRegistryAsync _handlerRegistryAsync;
        private readonly IQueryHandlerFactoryAsync _handlerFactoryAsync;
        private readonly IQueryHandlerDecoratorFactoryAsync _decoratorFactoryAsync;

        private readonly IPolicyRegistry<string> _policyRegistry;
        private readonly ResiliencePipelineProvider<string> _resiliencePipelineProvider;

        private readonly IAmADarkerTracer? _tracer;
        private readonly InstrumentationOptions _instrumentationOptions;

        public QueryProcessor(
            IHandlerConfiguration handlerConfiguration,
            IQueryContextFactory queryContextFactory,
            IPolicyRegistry<string> policyRegistry = null,
            ResiliencePipelineProvider<string> resiliencePipelineProvider = null,
            IAmADarkerTracer? tracer = null,
            InstrumentationOptions instrumentationOptions = InstrumentationOptions.All)
        {
            if (handlerConfiguration == null)
                throw new ArgumentNullException(nameof(handlerConfiguration));

            _handlerRegistry = handlerConfiguration.HandlerRegistry ?? throw new ArgumentException($"{nameof(handlerConfiguration.HandlerRegistry)} must not be null", nameof(handlerConfiguration));
            _handlerFactory = handlerConfiguration.HandlerFactory ?? throw new ArgumentException($"{nameof(handlerConfiguration.HandlerFactory)} must not be null", nameof(handlerConfiguration));
            _decoratorFactory = handlerConfiguration.DecoratorFactory ?? throw new ArgumentException($"{nameof(handlerConfiguration.DecoratorFactory)} must not be null", nameof(handlerConfiguration));

            _handlerRegistryAsync = handlerConfiguration.HandlerRegistryAsync;
            _handlerFactoryAsync = handlerConfiguration.HandlerFactoryAsync;
            _decoratorFactoryAsync = handlerConfiguration.DecoratorFactoryAsync;

            _queryContextFactory = queryContextFactory ?? throw new ArgumentNullException(nameof(queryContextFactory));
            _policyRegistry = policyRegistry;
            _resiliencePipelineProvider = resiliencePipelineProvider;
            _tracer = tracer;
            _instrumentationOptions = instrumentationOptions;
        }

        public TResult Execute<TResult>(IQuery<TResult> query, IQueryContext queryContext = null)
        {
            using (var pipelineBuilder = new PipelineBuilder<TResult>(_handlerRegistry, _handlerFactory, _decoratorFactory))
            {
                if (queryContext == null)
                    queryContext = _queryContextFactory.Create();
                InitQueryContext(queryContext);

                var span = _tracer?.CreateQuerySpan(query, queryContext.Span, queryContext, _instrumentationOptions);
                queryContext.Span = span;
                queryContext.Tracer = _tracer;

                var entryPoint = pipelineBuilder.Build(query, queryContext, _instrumentationOptions);

                try
                {
                    return entryPoint.Invoke(query);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    _tracer?.AddExceptionToSpan(span, ex.InnerException);
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    throw; // never reached, but required by compiler
                }
                catch (Exception ex)
                {
                    _tracer?.AddExceptionToSpan(span, ex);
                    _logger.LogInformation(ex,"An exception was thrown during pipeline execution");
                    throw;
                }
                finally
                {
                    _tracer?.EndSpan(span);
                }
            }
        }

        public async Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, IQueryContext queryContext = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var pipelineBuilder = new PipelineBuilder<TResult>(
                _handlerRegistry, _handlerFactory, _decoratorFactory,
                _handlerRegistryAsync, _handlerFactoryAsync, _decoratorFactoryAsync))
            {
                if (queryContext == null)
                    queryContext = _queryContextFactory.Create();
                InitQueryContext(queryContext);

                var span = _tracer?.CreateQuerySpan(query, queryContext.Span, queryContext, _instrumentationOptions);
                queryContext.Span = span;
                queryContext.Tracer = _tracer;

                var entryPoint = pipelineBuilder.BuildAsync(query, queryContext, _instrumentationOptions);

                try
                {
                    _logger.LogDebug("Invoking async pipeline...");
                    return await entryPoint.Invoke(query, cancellationToken).ConfigureAwait(false);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    _tracer?.AddExceptionToSpan(span, ex.InnerException);
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    throw; // never reached, but required by compiler
                }
                catch (Exception ex)
                {
                    _tracer?.AddExceptionToSpan(span, ex);
                    _logger.LogInformation(ex,"An exception was thrown during async pipeline execution");
                    throw;
                }
                finally
                {
                    _tracer?.EndSpan(span);
                }
            }
        }

        private void InitQueryContext(IQueryContext queryContext)
        {
            if (queryContext.Policies == null)
                queryContext.Policies = _policyRegistry;
            if (queryContext.ResiliencePipeline == null)
                queryContext.ResiliencePipeline = _resiliencePipelineProvider;
        }
    }
}
