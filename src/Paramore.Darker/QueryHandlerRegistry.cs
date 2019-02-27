using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Paramore.Darker.Exceptions;

namespace Paramore.Darker
{
    public class QueryHandlerRegistry : IQueryHandlerRegistry
    {
        private readonly IDictionary<Type, Type> _registry;

        public QueryHandlerRegistry()
        {
            _registry = new Dictionary<Type, Type>();
        }

        public virtual Type Get(Type queryType)
        {
            return _registry.ContainsKey(queryType) ? _registry[queryType] : null;
        }

        public virtual  void Register<TQuery, TResult, THandler>()
            where TQuery : IQuery<TResult>
            where THandler : IQueryHandler<TQuery, TResult>
        {
            Register(typeof(TQuery), typeof(TResult), typeof(THandler));
        }

        public virtual void Register(Type queryType, Type resultType, Type handlerType)
        {
            if (_registry.ContainsKey(queryType))
                throw new ConfigurationException($"Registry already contains an entry for {queryType.Name}");


            if (!HasMatchingResultType(queryType, resultType))
                throw new ConfigurationException($"Result type not valid for query {queryType.Name}");

            _registry.Add(queryType, handlerType);
        }

        private static bool HasMatchingResultType(Type queryType, Type resultType)
        {
            return queryType.GetInterfaces().Any(i => i.GenericTypeArguments.Any(t => t == resultType));
        }


        public void RegisterFromAssemblies(IEnumerable<Assembly> assemblies)
        {
            var subscribers =
                from t in assemblies.SelectMany(a => a.ExportedTypes)
                let ti = t.GetTypeInfo()
                where ti.IsClass && !ti.IsAbstract && !ti.IsInterface
                from i in t.GetTypeInfo().ImplementedInterfaces
                where i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)
                select new
                {
                    QueryType = i.GenericTypeArguments.ElementAt(0),
                    ResultType = i.GenericTypeArguments.ElementAt(1),
                    HandlerType = t
                };

            foreach (var subscriber in subscribers)
            {
                Register(subscriber.QueryType, subscriber.ResultType, subscriber.HandlerType);
            }
        }
    }
}