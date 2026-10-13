#nullable enable annotations
using System;

namespace Paramore.Darker
{
    public interface IQueryHandlerFactory
    {
        IQueryHandler Create(Type handlerType, IAmALifetime lifetime);
        void Release(IQueryHandler handler, IAmALifetime lifetime);

        /// <summary>
        /// Creates the scope handle that owns the components of one pipeline execution, or returns
        /// <c>null</c> when the factory has no scope of its own to offer.
        /// </summary>
        /// <returns>The scope handle, or <c>null</c>.</returns>
        IAmAScope? CreatePipelineScope();
    }
}
