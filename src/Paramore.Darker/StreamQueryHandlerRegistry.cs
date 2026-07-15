using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Paramore.Darker.Exceptions;

namespace Paramore.Darker
{
    /// <summary>
    /// Maps stream query types to their stream handler types.
    /// </summary>
    public class StreamQueryHandlerRegistry : IStreamQueryHandlerRegistry
    {
        private readonly IDictionary<Type, IResolveHandlers> _registry = new Dictionary<Type, IResolveHandlers>();

        /// <inheritdoc/>
        public virtual Type Get(Type queryType, IQuery query, IQueryContext context) =>
            _registry.TryGetValue(queryType, out var route) ? route.ResolveHandlerType(query, context) : null;

        /// <inheritdoc/>
        public virtual void Register<TQuery, TResult, THandler>()
            where TQuery : IStreamQuery<TResult>
            where THandler : IStreamQueryHandler<TQuery, TResult>
        {
            Register(typeof(TQuery), typeof(TResult), typeof(THandler));
        }

        /// <inheritdoc/>
        public virtual void Register(Type queryType, Type resultType, Type handlerType)
        {
            if (_registry.ContainsKey(queryType))
                throw new ConfigurationException($"Registry already contains an entry for {queryType.Name}");

            if (!HasMatchingResultType(queryType, resultType))
                throw new ConfigurationException($"Result type not valid for query {queryType.Name}");

            _registry.Add(queryType, new FixedHandlerRoute(handlerType));
        }

        /// <summary>
        /// Scans the given assemblies and registers all public, concrete IStreamQueryHandler implementations.
        /// </summary>
        /// <inheritdoc/>
        public virtual void Register<TQuery, TResult>(
            Func<TQuery, IQueryContext, Type?> router,
            params Type[] candidateHandlerTypes)
            where TQuery : IStreamQuery<TResult>
        {
            var queryType = typeof(TQuery);
            if (_registry.ContainsKey(queryType))
                throw new ConfigurationException($"Registry already contains an entry for {queryType.Name}");

            var handlerInterface = typeof(IStreamQueryHandler<TQuery, TResult>);
            foreach (var candidate in candidateHandlerTypes)
            {
                if (!handlerInterface.IsAssignableFrom(candidate))
                    throw new ConfigurationException(
                        $"Candidate {candidate.Name} does not implement {handlerInterface.Name}");
            }

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
                from i in ti.ImplementedInterfaces
                where i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamQueryHandler<,>)
                select new
                {
                    QueryType = i.GenericTypeArguments.ElementAt(0),
                    ResultType = i.GenericTypeArguments.ElementAt(1),
                    HandlerType = t
                };

            foreach (var subscriber in subscribers)
                Register(subscriber.QueryType, subscriber.ResultType, subscriber.HandlerType);
        }

        private static bool HasMatchingResultType(Type queryType, Type resultType) =>
            queryType.GetInterfaces().Any(i => i.GenericTypeArguments.Any(t => t == resultType));
    }
}
