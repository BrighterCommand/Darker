using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A handler that records whether the container disposed it.
    /// </summary>
    public interface IDisposableTrackedHandler : IDisposable
    {
        bool IsDisposed { get; }
    }

    /// <summary>
    /// Container-managed singleton that collects each <see cref="IDisposableTrackedHandler"/> instance
    /// as it is constructed, so a test can count the instances created across queries and check which were disposed.
    /// </summary>
    public sealed class DisposableHandlerLog
    {
        private readonly ConcurrentQueue<IDisposableTrackedHandler> _handlers = new();

        public IReadOnlyList<IDisposableTrackedHandler> Handlers => _handlers.ToList();

        public void Record(IDisposableTrackedHandler handler) => _handlers.Enqueue(handler);
    }
}
