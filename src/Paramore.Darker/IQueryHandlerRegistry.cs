using System;

namespace Paramore.Darker
{
    public interface IQueryHandlerRegistry
    {
        /// <summary>
        /// Returns the handler type registered for <paramref name="queryType"/>.
        /// <list type="bullet">
        ///   <item>Absent query type — returns <c>null</c> (caller throws <see cref="Exceptions.ConfigurationException"/>).</item>
        ///   <item>Present, resolvable entry — returns the handler <see cref="Type"/>.</item>
        ///   <item>Present routed entry that resolves to <c>null</c> or a non-candidate — throws <see cref="Exceptions.RoutingException"/>.</item>
        /// </list>
        /// </summary>
        Type Get(Type queryType, IQuery query, IQueryContext context);

        void Register<TQuery, TResult, THandler>()
            where TQuery : IQuery<TResult>
            where THandler : IQueryHandler<TQuery, TResult>;

        void Register(Type queryType, Type resultType, Type handlerType);

        /// <summary>
        /// Registers an agreement-dispatch route for <typeparamref name="TQuery"/>: the
        /// <paramref name="router"/> is invoked per execution and selects one handler type
        /// from the declared <paramref name="candidateHandlerTypes"/>.
        /// </summary>
        /// <typeparam name="TQuery">The query type to route.</typeparam>
        /// <typeparam name="TResult">The result type returned by the query.</typeparam>
        /// <param name="router">A function that receives the query instance and the current
        /// <see cref="IQueryContext"/> and returns the handler <see cref="Type"/> to use for
        /// this execution. Must return a type from <paramref name="candidateHandlerTypes"/>.</param>
        /// <param name="candidateHandlerTypes">The set of handler types the router may select.
        /// Each must implement <see cref="IQueryHandler{TQuery,TResult}"/>.</param>
        /// <exception cref="Exceptions.ConfigurationException">Thrown if <typeparamref name="TQuery"/>
        /// is already registered, or if a candidate does not implement the handler interface.</exception>
        void Register<TQuery, TResult>(
            Func<TQuery, IQueryContext, Type?> router,
            params Type[] candidateHandlerTypes)
            where TQuery : IQuery<TResult>;
    }
}