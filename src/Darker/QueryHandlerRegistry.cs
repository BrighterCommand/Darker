using System;
using System.Collections.Generic;
using System.Linq;
using Darker.Exceptions;

#if NETSTANDARD1_0
using System.Reflection;
#endif

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
            Register(typeof(TRequest), typeof(TResponse), typeof(THandler));
        }

        public void RegisterAsync<TRequest, TResponse, THandler>()
            where TRequest : IQueryRequest<TResponse>
            where TResponse : IQueryResponse
            where THandler : IAsyncQueryHandler<TRequest, TResponse>
        {
            Register(typeof(TRequest), typeof(TResponse), typeof(THandler));
        }

        public void Register(Type requestType, Type responseType, Type handlerType)
        {
            if (_registry.ContainsKey(requestType))
                throw new ConfigurationException($"Registry already contains an entry for {requestType.Name}");

            if (!HasMatchingResponseType(requestType, responseType))
                throw new ConfigurationException($"Response type not valid for request {requestType.Name}");

            _registry.Add(requestType, handlerType);
        }

        private bool HasMatchingResponseType(Type requestType, Type responseType)
        {
            return requestType.GetInterfaces().Any(i => i.GenericTypeArguments.Any(t => t == responseType));
        }
    }
}