using System;
using System.Collections.Generic;

namespace Paramore.Darker.RemoteQueries.AzureFunctions
{ 
    public sealed class AzureQueryRegistry : IRemoteQueryRegistry
    {
        private readonly AzureConfig _config;
        private readonly IDictionary<Type, Func<IQueryHandler>> _handlerFactories;

        public AzureQueryRegistry(string baseUri, string functionsKey)
        {
            _handlerFactories = new Dictionary<Type, Func<IQueryHandler>>();
            _config = new AzureConfig
            {
                BaseUri = new Uri(baseUri),
                FunctionsKey = functionsKey
            };
        }

        public bool CanHandle(Type query) => _handlerFactories.ContainsKey(query);

        public IQueryHandler ResolveHandler(Type query) => _handlerFactories[query]();

        public void Register<TQuery, TResult>(string name) where TQuery : IQuery<TResult>
        {
            _handlerFactories.Add(typeof(TQuery), () => new AzureRemoteQueryHandler<TQuery, TResult>(_config, name));
        }
    }
}