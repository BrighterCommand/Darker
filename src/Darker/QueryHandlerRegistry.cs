using System;
using System.Collections.Generic;
using Darker.Exceptions;

namespace Darker
{
    public sealed class QueryHandlerRegistry : IQueryHandlerRegistry
    {
        private readonly IDictionary<Type, Type> _registry;

        public QueryHandlerRegistry()
        {
            _registry = new Dictionary<Type, Type>();
        }

        public Type Get(Type requestType)
        {
            return _registry.ContainsKey(requestType) ? _registry[requestType] : null;
        }

        public void Register<TRequest, TResponse, THandler>()
            where TRequest : IQueryRequest<TResponse>
            where TResponse : IQueryResponse
            where THandler : IQueryHandler<TRequest, TResponse>
        {
            var requestType = typeof(TRequest);

            if (_registry.ContainsKey(requestType))
                throw new ConfigurationException($"Registry already contains an entry for {requestType.Name}");

            _registry.Add(requestType, typeof(THandler));
        }
    }
}