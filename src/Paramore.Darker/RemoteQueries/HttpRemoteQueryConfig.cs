#if NETSTANDARD
using System;

namespace Paramore.Darker.RemoteQueries
{
    public class HttpRemoteQueryConfig
    {
        public Uri BaseUri { get; }
        public string ApiKey { get; }
        public string ApiKeyHeaderName { get; }
        public bool HasApiKey { get; }

        public HttpRemoteQueryConfig(Uri baseUri)
        {
            BaseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
            HasApiKey = false;
        }
        
        public HttpRemoteQueryConfig(Uri baseUri, string apiKey, string apiKeyHeaderName)
        {
            BaseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
            ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            ApiKeyHeaderName = apiKeyHeaderName ?? throw new ArgumentNullException(nameof(apiKeyHeaderName));
            HasApiKey = true;
        }
    }
}
#endif