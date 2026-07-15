using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Paramore.Darker.Exceptions;

namespace Paramore.Darker
{
    public class QueryHandlerRegistry : IQueryHandlerRegistry
    {
        private readonly IDictionary<Type, IResolveHandlers> _registry;

        public QueryHandlerRegistry()
        {
            _registry = new Dictionary<Type, IResolveHandlers>();
        }

        public virtual Type Get(Type queryType, IQuery query, IQueryContext context)
        {
            return _registry.TryGetValue(queryType, out var route) ? route.ResolveHandlerType(query, context) : null;
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

            _registry.Add(queryType, new FixedHandlerRoute(handlerType));
        }

        private static bool HasMatchingResultType(Type queryType, Type resultType)
        {
            return queryType.GetInterfaces().Any(i => i.GenericTypeArguments.Any(t => t == resultType));
        }


        public virtual void Register<TQuery, TResult>(
            Func<TQuery, IQueryContext, Type?> router,
            params Type[] candidateHandlerTypes)
            where TQuery : IQuery<TResult>
        {
            var queryType = typeof(TQuery);
            if (_registry.ContainsKey(queryType))
                throw new ConfigurationException($"Registry already contains an entry for {queryType.Name}");

            Func<IQuery, IQueryContext, Type?> typeErasedRouter = (q, ctx) => router((TQuery)q, ctx);
            _registry.Add(queryType, new RoutedHandlers(queryType, typeErasedRouter, candidateHandlerTypes));
        }

        public void RegisterFromAssemblies(IEnumerable<Assembly> assemblies)
        {
            // IMPORTANT: ExportedTypes is load-bearing — see ADR 0011 §9-10.
            // It ring-fences the scan to public types only so that internal
            // TestDoubles in test assemblies are not registered as handlers.
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