using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Paramore.Darker.Decorators;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Logging;

namespace Paramore.Darker.QueryLogging
{
    public class QueryLoggingDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILogger Logger = ApplicationLogging.CreateLogger<QueryLoggingDecorator<TQuery, TResult>>();

        private readonly JsonSerializerSettings? _serializerSettings;

        public QueryLoggingDecorator(JsonSerializerSettings? serializerSettings = null)
        {
            _serializerSettings = serializerSettings;
        }

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            // nothing to do
        }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            var sw = Stopwatch.StartNew();

            var queryName = query.GetType().Name;
            Logger.LogInformation("Executing query {QueryName}: {Query}", queryName, Serialize(query));

            var result = next(query);

            var withFallback = Context.Bag.ContainsKey(FallbackPolicyDecorator<TQuery, TResult>.CauseOfFallbackException)
                ? " (with fallback)"
                : string.Empty;

            Logger.LogInformation("Execution of query {QueryName} completed in {Elapsed}ms" + withFallback, queryName, sw.Elapsed.TotalMilliseconds);

            return result;
        }

        private string Serialize<T>(T value)
        {
            if (_serializerSettings == null)
                throw new ConfigurationException("No serializer settings are configured. Pass JsonSerializerSettings to the QueryLoggingDecorator constructor.");
            return JsonConvert.SerializeObject(value, _serializerSettings);
        }
    }
}