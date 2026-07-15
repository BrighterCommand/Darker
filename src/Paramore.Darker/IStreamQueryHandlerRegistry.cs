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
    }
}
