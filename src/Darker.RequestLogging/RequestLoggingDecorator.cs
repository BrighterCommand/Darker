using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Darker.Decorators;
using Darker.Exceptions;
using Darker.RequestLogging.Logging;

namespace Darker.RequestLogging
{
    public class RequestLoggingDecorator<TRequest, TResponse> : IQueryHandlerDecorator<TRequest, TResponse>
        where TRequest : IQueryRequest<TResponse>
        where TResponse : IQueryResponse
    {
        private static readonly ILog _logger = LogProvider.GetLogger(typeof(RequestLoggingDecorator<,>));

        public IRequestContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            // nothing to do
        }

        public TResponse Execute(TRequest request, Func<TRequest, TResponse> next, Func<TRequest, TResponse> fallback)
        {
            var sw = Stopwatch.StartNew();

            var queryName = request.GetType().Name;
            _logger.InfoFormat("Executing query {QueryName}: {Query}", queryName, GetSerializer().Serialize(request));

            var result = next(request);

            var withFallback = Context.Bag.ContainsKey(FallbackPolicyDecorator<TRequest, TResponse>.CauseOfFallbackException)
                ? " (with fallback)"
                : string.Empty;

            _logger.InfoFormat("Execution of query {QueryName} completed in {Elapsed}" + withFallback, queryName, sw.Elapsed);

            return result;
        }

        public async Task<TResponse> ExecuteAsync(TRequest request,
            Func<TRequest, CancellationToken, Task<TResponse>> next,
            Func<TRequest, CancellationToken, Task<TResponse>> fallback,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var sw = Stopwatch.StartNew();

            var queryName = request.GetType().Name;
            _logger.InfoFormat("Executing async query {QueryName}: {Query}", queryName, GetSerializer().Serialize(request));

            var result = await next(request, cancellationToken).ConfigureAwait(false);

            var withFallback = Context.Bag.ContainsKey(FallbackPolicyDecorator<TRequest, TResponse>.CauseOfFallbackException)
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