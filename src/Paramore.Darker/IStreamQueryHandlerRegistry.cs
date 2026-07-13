using System;

namespace Paramore.Darker
{
    /// <summary>
    /// Registry that maps stream query types to their stream handler types.
    /// </summary>
    public interface IStreamQueryHandlerRegistry
    {
        /// <summary>
        /// Returns the handler type registered for the given query type, or null if not registered.
        /// </summary>
        Type Get(Type queryType);

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
