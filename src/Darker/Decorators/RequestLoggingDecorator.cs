using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Darker.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Darker.Decorators
{
    public class RequestLoggingDecorator<TRequest, TResponse> : IQueryHandlerDecorator<TRequest, TResponse>
        where TRequest : IQueryRequest<TResponse>
        where TResponse : IQueryResponse
    {
        private static readonly ILog _logger = LogProvider.GetLogger(typeof(RequestLoggingDecorator<,>));

        // todo: maybe make some of these settings configurable?
        private static readonly JsonSerializerSettings _defaultSerialiserSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Include,
            DateFormatString = "O", // ISO 8601: 2009-06-15T13:45:30.0000000-07:00
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter()
            }
        };

        public IRequestContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            // nothing to do
        }

        public TResponse Execute(TRequest request, Func<TRequest, TResponse> next, Func<TRequest, TResponse> fallback)
        {
            var sw = Stopwatch.StartNew();
            var json = JsonConvert.SerializeObject(request, _defaultSerialiserSettings);

            _logger.InfoFormat("Executing query {0}: {1}", request.GetType().Name, json);

            var result = next(request);

            var withFallback = Context.Bag.ContainsKey(FallbackPolicyDecorator<TRequest, TResponse>.CauseOfFallbackException)
                ? " (with fallback)"
                : string.Empty;

            _logger.InfoFormat("Query execution completed in {0}" + withFallback, sw.Elapsed);

            return result;
        }

        public async Task<TResponse> ExecuteAsync(TRequest request, Func<TRequest, Task<TResponse>> next, Func<TRequest, Task<TResponse>> fallback)
        {
            var sw = Stopwatch.StartNew();
            var json = JsonConvert.SerializeObject(request, _defaultSerialiserSettings);

            _logger.InfoFormat("Executing async query {0}: {1}", request.GetType().Name, json);

            var result = await next(request).ConfigureAwait(false);

            var withFallback = Context.Bag.ContainsKey(FallbackPolicyDecorator<TRequest, TResponse>.CauseOfFallbackException)
                ? " (with fallback)"
                : string.Empty;

            _logger.InfoFormat("Query execution completed in {0}" + withFallback, sw.Elapsed);

            return result;
        }
    }
}