using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A query whose handler receives an <see cref="ITrackedDependency"/> by constructor injection
    /// and returns it in the result, so a test can assert the dependency's identity and disposal
    /// after the pipeline completes.
    /// </summary>
    public sealed class TrackedQuery : IQuery<TrackedQuery.Result>
    {
        public sealed class Result
        {
            public ITrackedDependency HandlerDependency { get; set; }
        }
    }

    public sealed class TrackedQueryHandler : QueryHandler<TrackedQuery, TrackedQuery.Result>
    {
        private readonly ITrackedDependency _dependency;

        public TrackedQueryHandler(ITrackedDependency dependency) => _dependency = dependency;

        public override TrackedQuery.Result Execute(TrackedQuery query)
            => new TrackedQuery.Result { HandlerDependency = _dependency };
    }

    public sealed class TrackedQueryHandlerAsync : QueryHandlerAsync<TrackedQuery, TrackedQuery.Result>
    {
        private readonly ITrackedDependency _dependency;

        public TrackedQueryHandlerAsync(ITrackedDependency dependency) => _dependency = dependency;

        public override Task<TrackedQuery.Result> ExecuteAsync(TrackedQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new TrackedQuery.Result { HandlerDependency = _dependency });
    }
}
