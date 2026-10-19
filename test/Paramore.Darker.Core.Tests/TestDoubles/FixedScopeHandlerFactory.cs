#nullable enable annotations
using System;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A handler factory that hands out the one <see cref="IAmAScope"/> it was constructed with.
    /// </summary>
    internal sealed class FixedScopeHandlerFactory : IQueryHandlerFactory, IQueryHandlerFactoryAsync
    {
        private readonly IAmAScope _scope;

        public FixedScopeHandlerFactory(IAmAScope scope) => _scope = scope;

        public IAmAScope? CreatePipelineScope() => _scope;

        public IQueryHandler Create(Type handlerType, IAmALifetime lifetime)
            => (IQueryHandler)Activator.CreateInstance(handlerType)!;

        public void Release(IQueryHandler handler, IAmALifetime lifetime)
        {
        }

        public ValueTask ReleaseAsync(IQueryHandler handler, IAmALifetime lifetime) => default;
    }
}
