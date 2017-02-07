using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Darker.Decorators;
using Darker.Exceptions;
using Darker.RequestLogging.Logging;

namespace Darker.RequestLogging
{
    public class RequestLoggingDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILog _logger = LogProvider.GetLogger(typeof(RequestLoggingDecorator<,>));

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            // nothing to do
        }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            var sw = Stopwatch.StartNew();

            var queryName = query.GetType().Name;
            _logger.InfoFormat("Executing query {QueryName}: {Query}", queryName, GetSerializer().Serialize(query));

            var result = next(query);

            var withFallback = Context.Bag.ContainsKey(FallbackPolicyDecorator<TQuery, TResult>.CauseOfFallbackException)
                ? " (with fallback)"
                : string.Empty;

            _logger.InfoFormat("Execution of query {QueryName} completed in {Elapsed}" + withFallback, queryName, sw.Elapsed);

            return result;
        }

        public async Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var sw = Stopwatch.StartNew();

            var queryName = query.GetType().Name;
            _logger.InfoFormat("Executing async query {QueryName}: {Query}", queryName, GetSerializer().Serialize(query));

            var result = await next(query, cancellationToken).ConfigureAwait(false);

            var withFallback = Context.Bag.ContainsKey(FallbackPolicyDecorator<TQuery, TResult>.CauseOfFallbackException)
                ? " (with fallback)"
                : string.Empty;

            _logger.InfoFormat("Async execution of query {QueryName} completed in {Elapsed}" + withFallback, queryName, sw.Elapsed);

            return result;
        }

        private NewtonsftJsonSerializer GetSerializer()
        {
            if (!Context.Bag.ContainsKey(Constants.ContextBagKey))
                throw new ConfigurationException($"Serializer does not exist in context bag with key {Constants.ContextBagKey}.");

            var serializer = Context.Bag[Constants.ContextBagKey] as NewtonsftJsonSerializer;
            if (serializer == null)
                throw new ConfigurationException($"The serializer in the context bag (with key {Constants.ContextBagKey}) must be of type {nameof(NewtonsftJsonSerializer)}, but is {Context.Bag[Constants.ContextBagKey].GetType()}.");

            return serializer;
        }
    }
}