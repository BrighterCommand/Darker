using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A query whose handler records its injected <see cref="ITrackedDependency"/> and then blocks
    /// on a release gate, so a test can hold two pipelines provably in flight at once (AC6). The
    /// handler signals <see cref="Started"/> once it has resolved its dependency and is parked, and
    /// returns only after <see cref="Release"/> is called — letting a test compare the two scoped
    /// dependencies for isolation and then complete one pipeline while the other is still running.
    /// </summary>
    public sealed class ConcurrentTrackedQuery : IQuery<ConcurrentTrackedQuery.Result>
    {
        private readonly TaskCompletionSource<bool> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Completes when the handler has resolved its dependency and is parked at the gate.</summary>
        public Task Started => _started.Task;

        /// <summary>The gate the handler awaits before returning.</summary>
        public Task ReleaseGate => _release.Task;

        /// <summary>The dependency the handler received, recorded before it parks.</summary>
        public ITrackedDependency HandlerDependency { get; set; }

        public void SignalStarted() => _started.TrySetResult(true);

        public void Release() => _release.TrySetResult(true);

        public sealed class Result
        {
            public ITrackedDependency HandlerDependency { get; set; }
        }
    }

    public sealed class ConcurrentTrackedQueryHandler : QueryHandler<ConcurrentTrackedQuery, ConcurrentTrackedQuery.Result>
    {
        private readonly ITrackedDependency _dependency;

        public ConcurrentTrackedQueryHandler(ITrackedDependency dependency) => _dependency = dependency;

        public override ConcurrentTrackedQuery.Result Execute(ConcurrentTrackedQuery query)
        {
            query.HandlerDependency = _dependency;
            query.SignalStarted();
            query.ReleaseGate.GetAwaiter().GetResult();
            return new ConcurrentTrackedQuery.Result { HandlerDependency = _dependency };
        }
    }

    public sealed class ConcurrentTrackedQueryHandlerAsync : QueryHandlerAsync<ConcurrentTrackedQuery, ConcurrentTrackedQuery.Result>
    {
        private readonly ITrackedDependency _dependency;

        public ConcurrentTrackedQueryHandlerAsync(ITrackedDependency dependency) => _dependency = dependency;

        public override async Task<ConcurrentTrackedQuery.Result> ExecuteAsync(ConcurrentTrackedQuery query,
            CancellationToken cancellationToken = default)
        {
            query.HandlerDependency = _dependency;
            query.SignalStarted();
            await query.ReleaseGate;
            return new ConcurrentTrackedQuery.Result { HandlerDependency = _dependency };
        }
    }
}
