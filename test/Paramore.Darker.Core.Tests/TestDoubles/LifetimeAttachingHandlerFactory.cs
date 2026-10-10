using System;
using System.Collections.Generic;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A handler factory with <see cref="SimpleHandlerFactory"/> semantics that, when it creates a handler,
    /// also attaches the supplied disposables to the pipeline's <see cref="IAmALifetime"/> (in order).
    /// </summary>
    internal sealed class LifetimeAttachingHandlerFactory : IQueryHandlerFactory, IQueryHandlerFactoryAsync
    {
        private readonly Func<Type, IQueryHandler> _create;
        private readonly IReadOnlyList<IDisposable> _toAttach;

        public LifetimeAttachingHandlerFactory(Func<Type, IQueryHandler> create, params IDisposable[] toAttach)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _toAttach = toAttach;
        }

        public IQueryHandler Create(Type handlerType, IAmALifetime lifetime)
        {
            foreach (var disposable in _toAttach)
                lifetime.Add(disposable);

            return _create(handlerType);
        }

        public void Release(IQueryHandler handler, IAmALifetime lifetime)
        {
            if (handler is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
