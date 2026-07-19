using System;

namespace Paramore.Darker
{
    /// <summary>
    /// Registry that maps stream query types to their stream handler types.
    /// </summary>
    public interface IStreamQueryHandlerRegistry
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

        /// <summary>
        /// Registers a stream handler for a stream query using generic type parameters.
        /// </summary>
        void Register<TQuery, TResult, THandler>()
            where TQuery : IStreamQuery<TResult>
            where THandler : IStreamQueryHandler<TQuery, TResult>;

        /// <summary>
        /// Registers a stream handler for a stream query using runtime types.
        /// </summary>
        void Register(Type queryType, Type resultType, Type handlerType);

        /// <summary>
        /// Registers a routing function that selects a stream handler type from
        /// <paramref name="candidateHandlerTypes"/> at execution time based on the query
        /// content and/or the <see cref="IQueryContext"/>.
        /// </summary>
        /// <typeparam name="TQuery">The stream query type.</typeparam>
        /// <typeparam name="TResult">The result element type.</typeparam>
        /// <param name="router">
        /// A function that receives the query and context and returns the handler type to use.
        /// Returning <c>null</c> throws <see cref="Exceptions.RoutingException"/> with
        /// <see cref="Exceptions.RoutingFailure.NoHandlerResolved"/>.
        /// Returning a type outside <paramref name="candidateHandlerTypes"/> throws
        /// <see cref="Exceptions.RoutingException"/> with
        /// <see cref="Exceptions.RoutingFailure.UnregisteredCandidate"/>.
        /// </param>
        /// <param name="candidateHandlerTypes">
        /// The exhaustive set of handler types the router may return.
        /// Each must implement <see cref="IStreamQueryHandler{TQuery,TResult}"/>.
        /// </param>
        /// <exception cref="Exceptions.ConfigurationException">
        /// Thrown at registration time if the query type is already registered or if
        /// any candidate does not implement <see cref="IStreamQueryHandler{TQuery,TResult}"/>.
        /// </exception>
        void Register<TQuery, TResult>(
            Func<TQuery, IQueryContext, Type?> router,
            params Type[] candidateHandlerTypes)
            where TQuery : IStreamQuery<TResult>;
    }
}
