using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A stream query whose handler <em>and</em> stream decorator both receive an
    /// <see cref="ITrackedDependency"/> by constructor injection and record it on the query. The
    /// handler then parks on a release gate, so a test can hold two streams provably in flight at once.
    /// </summary>
    public sealed class TrackedStreamQuery : IStreamQuery<string>
    {
        private readonly TaskCompletionSource<bool> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Completes when the handler has recorded its dependency and is parked at the gate.</summary>
        public Task Started => _started.Task;

        /// <summary>The gate the handler awaits before yielding its item.</summary>
        public Task ReleaseGate => _release.Task;

        /// <summary>The dependency the handler received.</summary>
        public ITrackedDependency HandlerDependency { get; set; }

        /// <summary>The dependency the decorator received.</summary>
        public ITrackedDependency DecoratorDependency { get; set; }

        public void SignalStarted() => _started.TrySetResult(true);

        public void Release() => _release.TrySetResult(true);
    }
}
