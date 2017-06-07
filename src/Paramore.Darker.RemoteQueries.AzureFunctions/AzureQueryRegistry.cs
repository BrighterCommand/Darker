using System;

namespace Paramore.Darker.RemoteQueries.AzureFunctions
{ 
    public sealed class AzureQueryRegistry : HttpRemoteQueryRegistry
    {
        private readonly IRemoteQuerySerializer _serializer;
        private readonly HttpRemoteQueryConfig _config;

        public AzureQueryRegistry(IRemoteQuerySerializer serializer, string baseUri, string functionsKey)
        {
            _serializer = serializer;
            _config = new HttpRemoteQueryConfig(new Uri(baseUri), functionsKey, "X-Functions-Key");
        }

        public void Register<TQuery, TResult>(string functionName) where TQuery : IRemoteQuery<TResult>
        {
            HandlerFactories.Add(typeof(TQuery), () => new HttpRemoteQueryHandler<TQuery, TResult>(_serializer, _config, functionName));
        }
    }
}