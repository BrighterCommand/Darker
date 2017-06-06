using System;
using System.Collections.Generic;

namespace Paramore.Darker.RemoteQueries.AwsLambda
{ 
    public sealed class AwsQueryRegistry : IRemoteQueryRegistry
    {
        private readonly AwsConfig _config;
        private readonly IDictionary<Type, Func<IQueryHandler>> _handlerFactories;

        public AwsQueryRegistry(string baseUri, string apiKey)
        {
            _handlerFactories = new Dictionary<Type, Func<IQueryHandler>>();
            _config = new AwsConfig
            {
                BaseUri = new Uri(baseUri),
                ApiKey = apiKey
            };
        }

        public bool CanHandle(Type query) => _handlerFactories.ContainsKey(query);

        public IQueryHandler ResolveHandler(Type query) => _handlerFactories[query]();

        public void Register<TQuery, TResult>(string name) where TQuery : IQuery<TResult>
        {
            _handlerFactories.Add(typeof(TQuery), () => new AwsRemoteQueryHandler<TQuery, TResult>(_config, name));
        }
    }
}