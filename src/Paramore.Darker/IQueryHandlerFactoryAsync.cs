#nullable enable annotations
using System;
using System.Threading.Tasks;

namespace Paramore.Darker
{
    public interface IQueryHandlerFactoryAsync
    {
        IQueryHandler Create(Type handlerType, IAmALifetime lifetime);

        /// <summary>
        /// Releases a handler that this factory created, once the async or stream pipeline has finished with it.
        /// </summary>
        /// <remarks>
        /// The factory decides whether release means disposal: a container-backed factory typically does
        /// nothing, because the container owns the handler, while a simple factory disposes it. The pipeline
        /// contains and logs any fault thrown here, so a faulty release cannot change the query's outcome.
        /// The method takes no cancellation token, because teardown must run to completion even when the
        /// query was cancelled.
        /// </remarks>
        /// <param name="handler">The handler to release.</param>
        /// <param name="lifetime">The per-query lifetime the handler was created under.</param>
        ValueTask ReleaseAsync(IQueryHandler handler, IAmALifetime lifetime);

        /// <summary>
        /// Creates the scope handle that owns the components of one pipeline execution, or returns
        /// <c>null</c> when the factory has no scope of its own to offer.
        /// </summary>
        /// <returns>The scope handle, or <c>null</c>.</returns>
        IAmAScope? CreatePipelineScope();
    }
}
