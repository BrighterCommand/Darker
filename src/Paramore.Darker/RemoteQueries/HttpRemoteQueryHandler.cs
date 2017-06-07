#if NETSTANDARD
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Logging;

namespace Paramore.Darker.RemoteQueries
{
    public sealed class HttpRemoteQueryHandler<TQuery, TResult> : IQueryHandler
        where TQuery : IRemoteQuery<TResult>
    {
        private static readonly ILog _logger = LogProvider.GetLogger(typeof(HttpRemoteQueryHandler<,>));

        private readonly IRemoteQuerySerializer _serializer;
        private readonly HttpRemoteQueryConfig _config;
        private readonly string _functionName;
        
        public IQueryContext Context { get; set; }

        public HttpRemoteQueryHandler(IRemoteQuerySerializer serializer, HttpRemoteQueryConfig config, string functionName)
        {
            _serializer = serializer;
            _config = config;
            _functionName = functionName;
        }

        public async Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = _config.BaseUri;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_serializer.MediaType));
                client.DefaultRequestHeaders.Add(_config.ApiKeyHeaderName, _config.ApiKey);

                var sw = Stopwatch.StartNew();

                // todo use streams for post body
                var json = _serializer.Serialize(query);
                var content = new StringContent(json, Encoding.UTF8, _serializer.MediaType);

                // todo error handling
                using (var response = await client.PostAsync(_functionName, content, cancellationToken).ConfigureAwait(false))
                using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    var result = _serializer.Deserialize<TResult>(responseStream);
                    
                    _logger.InfoFormat("Execution of Azure Function {FunctionName} completed in {Elapsed}", _functionName, sw.Elapsed);

                    return result;
                }
            }
        }
    }
}
#endif