using System;

namespace Paramore.Darker
{
    /// <summary>
    /// An <see cref="IResolveHandlers"/> implementation that always returns the same handler
    /// type, ignoring the query instance and the query context.  This encapsulates the
    /// type-based resolution behaviour that existed before agreement dispatch was introduced.
    /// </summary>
    public sealed class FixedHandlerRoute : IResolveHandlers
    {
        private readonly Type _handlerType;

        public FixedHandlerRoute(Type handlerType)
        {
            _handlerType = handlerType;
        }

        /// <inheritdoc/>
        public Type ResolveHandlerType(IQuery query, IQueryContext context) => _handlerType;
    }
}
