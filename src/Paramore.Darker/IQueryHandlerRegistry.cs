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
    }
}