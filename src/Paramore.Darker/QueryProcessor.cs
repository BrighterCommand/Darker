using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

            _handlerRegistry = handlerConfiguration.HandlerRegistry ?? throw new ArgumentException($"{nameof(handlerConfiguration.HandlerRegistry)} must not be null", nameof(handlerConfiguration));
            _handlerFactory = handlerConfiguration.HandlerFactory ?? throw new ArgumentException($"{nameof(handlerConfiguration.HandlerFactory)} must not be null", nameof(handlerConfiguration));
            _decoratorFactory = handlerConfiguration.DecoratorFactory ?? throw new ArgumentException($"{nameof(handlerConfiguration.DecoratorFactory)} must not be null", nameof(handlerConfiguration));

            _queryContextFactory = queryContextFactory ?? throw new ArgumentNullException(nameof(queryContextFactory));
            _contextBagData = contextBagData ?? new Dictionary<string, object>();
        }

        public TResult Execute<TResult>(IQuery<TResult> query)
        {
            using (var pipelineBuilder = new PipelineBuilder<TResult>(_handlerRegistry, _handlerFactory, _decoratorFactory))
            {
                var queryContext = CreateQueryContext();
                var pipeline = pipelineBuilder.Build(query, queryContext);

                try
                {
                    return pipeline.Last().Invoke(query);
                }
                catch (Exception ex)
                {
                    _logger.InfoException("An exception was thrown during pipeline execution", ex);
                    throw;
                }
            }
        }

        public async Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var pipelineBuilder = new PipelineBuilder<TResult>(_handlerRegistry, _handlerFactory, _decoratorFactory))
            {
                var queryContext = CreateQueryContext();
                var pipeline = pipelineBuilder.BuildAsync(query, queryContext);

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
        }

        private IQueryContext CreateQueryContext()
        {
            _logger.Debug("Creating query context...");

            var queryContext = _queryContextFactory.Create();

            // todo: no need for IQueryContext i think. just use dictionary
            queryContext.Bag = _contextBagData.ToDictionary(d => d.Key, d => d.Value); // shallow copy

            return queryContext;
        }
    }
}