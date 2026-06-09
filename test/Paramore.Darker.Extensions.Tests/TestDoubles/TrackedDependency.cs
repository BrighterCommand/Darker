using System;
using System.Threading;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// Observability probe for the lifetime acceptance tests. A dependency that records its own
    /// construction (via the shared <see cref="DependencyTracker"/>) and whether it has been
    /// disposed, so a test can assert how many were created across queries and whether Darker
    /// disposed them (requirements "Key Terms and Observability").
    /// </summary>
    public interface ITrackedDependency : IDisposable
    {
        bool IsDisposed { get; }
    }

    public sealed class TrackedDependency : ITrackedDependency
    {
        public TrackedDependency(DependencyTracker tracker)
        {
            tracker.RecordConstruction();
        }

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    /// <summary>
    /// Shared, container-managed singleton that counts how many <see cref="TrackedDependency"/>
    /// instances have been constructed. Thread-safe so the concurrency acceptance test (AC6) can
    /// rely on it across pipelines running in parallel.
    /// </summary>
    public sealed class DependencyTracker
    {
        private int _constructionCount;

        public int ConstructionCount => Volatile.Read(ref _constructionCount);

        public void RecordConstruction() => Interlocked.Increment(ref _constructionCount);
    }
}
