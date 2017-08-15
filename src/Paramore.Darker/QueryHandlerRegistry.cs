using System;
using System.Collections.Generic;
using Paramore.Darker.Exceptions;

#if !NETSTANDARD1_0
using System.Reflection;
using System.Linq;
#endif

namespace Paramore.Darker
{
    public sealed class QueryHandlerRegistry : IQueryHandlerRegistry
    {
        private readonly IDictionary<Type, Type> _registry;

        public QueryHandlerRegistry()
        {
            _registry = new Dictionary<Type, Type>();
        }

        public Type Get(Type queryType)
        {
            return _registry.ContainsKey(queryType) ? _registry[queryType] : null;
        }

        public void Register<TQuery, TResult, THandler>()
            where TQuery : IQuery<TResult>
            where THandler : IQueryHandler<TQuery, TResult>
        {
            Register(typeof(TQuery), typeof(TResult), typeof(THandler));
        }

        public void Register(Type queryType, Type resultType, Type handlerType)
        {
            if (_registry.ContainsKey(queryType))
                throw new ConfigurationException($"Registry already contains an entry for {queryType.Name}");

#if !NETSTANDARD1_0
            if (!HasMatchingResultType(queryType, resultType))
                throw new ConfigurationException($"Result type not valid for query {queryType.Name}");
#endif

            _registry.Add(queryType, handlerType);
        }

#if !NETSTANDARD1_0
        private static bool HasMatchingResultType(Type queryType, Type resultType)
        {
            return queryType.GetInterfaces().Any(i => i.GenericTypeArguments.Any(t => t == resultType));
        }
#endif
    }
}