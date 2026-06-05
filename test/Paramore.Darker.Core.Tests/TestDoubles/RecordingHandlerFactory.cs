using System;
using System.Collections.Generic;
using System.Linq;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A handler factory double that creates handlers via a delegate (exactly like
    /// <see cref="SimpleHandlerFactory"/>) and additionally records which handlers it
    /// released. This lets tests express the legacy
    /// <c>Verify(x =&gt; x.Release(handler), Times.Once/Never)</c> assertions as state
    /// (<see cref="ReleaseCount"/> / <see cref="Released"/>) — ADR 0013, Decision 4.
    /// One class implements both the sync and async factory interfaces (they are
    /// identical — no CreateAsync).
    /// </summary>
    internal class RecordingHandlerFactory : IQueryHandlerFactory, IQueryHandlerFactoryAsync
    {
        private readonly Func<Type, IQueryHandler> _create;
        private readonly List<IQueryHandler> _released = new List<IQueryHandler>();

        public RecordingHandlerFactory(Func<Type, IQueryHandler> create)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
        }

        /// <summary>The handlers passed to <see cref="Release"/>, in release order.</summary>
        public IReadOnlyList<IQueryHandler> Released => _released;

        /// <summary>How many times the given handler instance was released (reference equality).</summary>
        public int ReleaseCount(IQueryHandler handler) => _released.Count(r => ReferenceEquals(r, handler));

        public IQueryHandler Create(Type handlerType) => _create(handlerType);

        public void Release(IQueryHandler handler)
        {
            _released.Add(handler);
            if (handler is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
