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
        /// <remarks>
        /// <para>
        /// The pipeline builder calls this once per build, on the factory that creates the handler, and
        /// before any <c>Create</c> call. The handle reaches every <c>Create</c> call of that pipeline as
        /// <see cref="IAmALifetime.PipelineScope"/>, and is disposed with the lifetime.
        /// </para>
        /// <para>
        /// <b>Each call must return a distinct handle that is never shared between queries.</b> A handle
        /// that outlived or spanned queries would be disposed after the first of them.
        /// </para>
        /// <para>
        /// Return <c>null</c> when the factory resolves nothing per query. A container-backed factory
        /// that needs the handle must throw <see cref="Exceptions.ConfigurationException"/> in
        /// <c>Create</c> when <see cref="IAmALifetime.PipelineScope"/> is null or not its own handle,
        /// rather than create a scope of its own.
        /// </para>
        /// </remarks>
        /// <returns>The scope handle, or <c>null</c>.</returns>
        IAmAScope? CreatePipelineScope();
    }
}
