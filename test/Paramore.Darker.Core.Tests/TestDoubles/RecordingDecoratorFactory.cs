using System;
using System.Collections.Generic;
using System.Linq;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A decorator factory double that creates decorators via a delegate (exactly like
    /// <see cref="SimpleHandlerDecoratorFactory"/>) and additionally records which
    /// decorators it released. FallbackPolicyTests asserts
    /// <c>Verify(x =&gt; x.Release&lt;T&gt;(decorator), Times.Once)</c>, and
    /// <see cref="SimpleHandlerDecoratorFactory.Release{T}"/> disposes but records
    /// nothing — so this sibling exposes <see cref="ReleaseCount"/> to re-express those
    /// assertions as state (ADR 0013, Decision 4 addendum). One class implements both
    /// the sync and async decorator-factory interfaces.
    /// </summary>
    internal class RecordingDecoratorFactory : IQueryHandlerDecoratorFactory, IQueryHandlerDecoratorFactoryAsync
    {
        private readonly Func<Type, IQueryHandlerDecorator> _create;
        private readonly List<IQueryHandlerDecorator> _released = new List<IQueryHandlerDecorator>();

        public RecordingDecoratorFactory(Func<Type, IQueryHandlerDecorator> create)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
        }

        /// <summary>The decorators passed to <see cref="Release{T}"/>, in release order.</summary>
        public IReadOnlyList<IQueryHandlerDecorator> Released => _released;

        /// <summary>How many times the given decorator instance was released (reference equality).</summary>
        public int ReleaseCount(IQueryHandlerDecorator decorator) => _released.Count(r => ReferenceEquals(r, decorator));

        public T Create<T>(Type decoratorType, IAmALifetime lifetime) where T : IQueryHandlerDecorator
            => (T)_create(decoratorType);

        public void Release<T>(T handler, IAmALifetime lifetime) where T : IQueryHandlerDecorator
        {
            _released.Add(handler);
            if (handler is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
