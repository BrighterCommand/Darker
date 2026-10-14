#nullable enable annotations
using System;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A handler factory with <see cref="SimpleHandlerFactory"/> semantics that
    /// supplies the given pipeline scope handle from <see cref="CreatePipelineScope"/>.
    /// </summary>
    internal sealed class LifetimeAttachingHandlerFactory : IQueryHandlerFactory, IQueryHandlerFactoryAsync
    {
        private readonly Func<Type, IQueryHandler> _create;
        private readonly IAmAScope _scope;

        public LifetimeAttachingHandlerFactory(Func<Type, IQueryHandler> create, IAmAScope scope)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _scope = scope;
        }

        public IQueryHandler Create(Type handlerType, IAmALifetime lifetime)
        {
            return _create(handlerType);
        }

        public IAmAScope? CreatePipelineScope() => _scope;

        public void Release(IQueryHandler handler, IAmALifetime lifetime)
        {
            if (handler is IDisposable disposable)
                disposable.Dispose();
        }

        public async ValueTask ReleaseAsync(IQueryHandler handler, IAmALifetime lifetime)
        {
            if (handler is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else if (handler is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
