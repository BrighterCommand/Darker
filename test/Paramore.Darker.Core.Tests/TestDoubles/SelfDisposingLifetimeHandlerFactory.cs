#nullable enable annotations
using System;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A handler factory that supplies a pipeline scope handle,
    /// and disposes the lifetime itself on release, ahead of the pipeline builder's own final dispose.
    /// </summary>
    internal sealed class SelfDisposingLifetimeHandlerFactory : IQueryHandlerFactory, IQueryHandlerFactoryAsync
    {
        private readonly Func<Type, IQueryHandler> _create;
        private readonly IAmAScope _scope;

        public SelfDisposingLifetimeHandlerFactory(Func<Type, IQueryHandler> create, IAmAScope scope)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _scope = scope;
        }

        public IQueryHandler Create(Type handlerType, IAmALifetime lifetime)
        {
            return _create(handlerType);
        }

        public IAmAScope? CreatePipelineScope() => _scope;

        public void Release(IQueryHandler handler, IAmALifetime lifetime) => lifetime.Dispose();

        public ValueTask ReleaseAsync(IQueryHandler handler, IAmALifetime lifetime) => lifetime.DisposeAsync();
    }
}
