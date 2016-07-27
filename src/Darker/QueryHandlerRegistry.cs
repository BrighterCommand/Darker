using System;
using System.Collections.Generic;

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
            return _registry[requestType];
        }

        public void Register<TRequest, TResponse, THandler>()
            where TRequest : IQueryRequest<TResponse>
            where TResponse : IQueryResponse
            where THandler : IQueryHandler<TRequest, TResponse>
        {
            _registry.Add(typeof(TRequest), typeof(THandler));
        }
    }
}