using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Paramore.Darker.Decorators;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Logging;

namespace Paramore.Darker.QueryLogging
{
    public class QueryLoggingDecoratorAsync<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILogger Logger = ApplicationLogging.CreateLogger<QueryLoggingDecoratorAsync<TQuery, TResult>>();

        private readonly JsonSerializerSettings _serializerSettings;

        public QueryLoggingDecoratorAsync(JsonSerializerSettings serializerSettings = null)
        {
            _serializerSettings = serializerSettings;
        }

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            // nothing to do
        }

        public async Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var sw = Stopwatch.StartNew();

            var queryName = query.GetType().Name;
            Logger.LogInformation("Executing async query {QueryName}: {Query}", queryName, Serialize(query));

            var result = await next(query, cancellationToken).ConfigureAwait(false);

            var withFallback = Context.Bag.ContainsKey(FallbackPolicyDecoratorAsync<TQuery, TResult>.CauseOfFallbackException)
                ? " (with fallback)"
                : string.Empty;

            Logger.LogInformation("Async execution of query {QueryName} completed in {Elapsed}ms" + withFallback, queryName, sw.Elapsed.TotalMilliseconds);

            return result;
        }

        private string Serialize<T>(T value)
        {
            if (_serializerSettings == null)
                throw new ConfigurationException("No serializer settings are configured. Pass JsonSerializerSettings to the QueryLoggingDecoratorAsync constructor.");
            return JsonConvert.SerializeObject(value, _serializerSettings);
        }
    }
}
