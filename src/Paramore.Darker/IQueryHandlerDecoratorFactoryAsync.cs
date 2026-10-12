using System;
using System.Threading.Tasks;

namespace Paramore.Darker
{
    public interface IQueryHandlerDecoratorFactoryAsync
    {
        T Create<T>(Type decoratorType, IAmALifetime lifetime) where T : IQueryHandlerDecorator;

        /// <summary>
        /// Releases a decorator that this factory created, once the async or stream pipeline has finished with it.
        /// </summary>
        /// <remarks>
        /// The factory decides whether release means disposal: a container-backed factory typically does
        /// nothing, because the container owns the decorator, while a simple factory disposes it. The pipeline
        /// contains and logs any fault thrown here, so a faulty release cannot change the query's outcome.
        /// The method takes no cancellation token, because teardown must run to completion even when the
        /// query was cancelled.
        /// </remarks>
        /// <typeparam name="T">The decorator type.</typeparam>
        /// <param name="handler">The decorator to release.</param>
        /// <param name="lifetime">The per-query lifetime the decorator was created under.</param>
        ValueTask ReleaseAsync<T>(T handler, IAmALifetime lifetime) where T : IQueryHandlerDecorator;
    }
}
