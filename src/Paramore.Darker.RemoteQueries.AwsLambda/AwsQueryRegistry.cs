using System;

namespace Paramore.Darker.RemoteQueries.AwsLambda
{ 
    public sealed class AwsQueryRegistry : HttpRemoteQueryRegistry
    {
        private readonly IRemoteQuerySerializer _serializer;
        private readonly HttpRemoteQueryConfig _config;

        public AwsQueryRegistry(IRemoteQuerySerializer serializer, string baseUri, string functionsKey)
        {
            _serializer = serializer;
            _config = new HttpRemoteQueryConfig(new Uri(baseUri), functionsKey, "X-Api-Key");
        }

        public void Register<TQuery, TResult>(string functionName) where TQuery : IRemoteQuery<TResult>
        {
            HandlerFactories.Add(typeof(TQuery), () => new HttpRemoteQueryHandler<TQuery, TResult>(_serializer, _config, functionName));
        }
    }
}