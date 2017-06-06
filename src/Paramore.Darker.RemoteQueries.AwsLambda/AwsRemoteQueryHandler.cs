using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Paramore.Darker.RemoteQueries.AwsLambda.Logging;

namespace Paramore.Darker.RemoteQueries.AwsLambda
{
    public sealed class AwsRemoteQueryHandler<TQuery, TResult> : QueryHandlerAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILog _logger = LogProvider.GetLogger(typeof(AwsRemoteQueryHandler<,>));
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly AwsConfig _config;
        private readonly string _functionName;

        public AwsRemoteQueryHandler(AwsConfig config, string functionName)
        {
            _config = config;
            _functionName = functionName;
        }

        public override async Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = _config.BaseUri;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("X-Api-Key", _config.ApiKey);

                var sw = Stopwatch.StartNew();

                // todo use streams for post body
                var json = JsonConvert.SerializeObject(query, _serializerSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // todo error handling
                using (var response = await client.PostAsync(_functionName, content, cancellationToken).ConfigureAwait(false))
                using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var streamReader = new StreamReader(responseStream))
                using (var reader = new JsonTextReader(streamReader))
                {
                    var result = new JsonSerializer().Deserialize<TResult>(reader);

                    _logger.InfoFormat("Execution of Lambda function {LambdaName} completed in {Elapsed}",
                        _functionName, sw.Elapsed);

                    return result;
                }
            }
        }
    }
}
