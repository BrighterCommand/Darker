using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A handler factory that attaches a disposable to the pipeline's <see cref="IAmALifetime"/> on create,
    /// and disposes the lifetime itself on release, ahead of the pipeline builder's own final dispose.
    /// </summary>
    internal sealed class SelfDisposingLifetimeHandlerFactory : IQueryHandlerFactory, IQueryHandlerFactoryAsync
    {
        private readonly Func<Type, IQueryHandler> _create;
        private readonly IDisposable _toAttach;

        public SelfDisposingLifetimeHandlerFactory(Func<Type, IQueryHandler> create, IDisposable toAttach)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _toAttach = toAttach;
        }

        public IQueryHandler Create(Type handlerType, IAmALifetime lifetime)
        {
            lifetime.Add(_toAttach);
            return _create(handlerType);
        }

        public void Release(IQueryHandler handler, IAmALifetime lifetime) => lifetime.Dispose();
    }
}
