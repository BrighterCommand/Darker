using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A query whose handler records its injected <see cref="ITrackedDependency"/> and then throws,
    /// so a test can assert the per-query dependency is still disposed when the pipeline fails (AC7).
    /// </summary>
    public sealed class ThrowingTrackedQuery : IQuery<ThrowingTrackedQuery.Result>
    {
        public ITrackedDependency HandlerDependency { get; set; }

        public sealed class Result { }
    }

    public sealed class ThrowingTrackedQueryHandler : QueryHandler<ThrowingTrackedQuery, ThrowingTrackedQuery.Result>
    {
        private readonly ITrackedDependency _dependency;

        public ThrowingTrackedQueryHandler(ITrackedDependency dependency) => _dependency = dependency;

        public override ThrowingTrackedQuery.Result Execute(ThrowingTrackedQuery query)
        {
            query.HandlerDependency = _dependency;
            throw new InvalidOperationException("Handler failed");
        }
    }

    public sealed class ThrowingTrackedQueryHandlerAsync : QueryHandlerAsync<ThrowingTrackedQuery, ThrowingTrackedQuery.Result>
    {
        private readonly ITrackedDependency _dependency;

        public ThrowingTrackedQueryHandlerAsync(ITrackedDependency dependency) => _dependency = dependency;

        public override async Task<ThrowingTrackedQuery.Result> ExecuteAsync(ThrowingTrackedQuery query,
            CancellationToken cancellationToken = default)
        {
            query.HandlerDependency = _dependency;
            await Task.Yield();
            throw new InvalidOperationException("Handler failed");
        }
    }

    /// <summary>
    /// A query whose async handler records its injected <see cref="ITrackedDependency"/> and then
    /// observes cancellation, so a test can assert disposal on the cancelled path (AC7).
    /// </summary>
    public sealed class CancellingTrackedQuery : IQuery<CancellingTrackedQuery.Result>
    {
        public ITrackedDependency HandlerDependency { get; set; }

        public sealed class Result { }
    }

    public sealed class CancellingTrackedQueryHandlerAsync : QueryHandlerAsync<CancellingTrackedQuery, CancellingTrackedQuery.Result>
    {
        private readonly ITrackedDependency _dependency;

        public CancellingTrackedQueryHandlerAsync(ITrackedDependency dependency) => _dependency = dependency;

        public override async Task<CancellingTrackedQuery.Result> ExecuteAsync(CancellingTrackedQuery query,
            CancellationToken cancellationToken = default)
        {
            query.HandlerDependency = _dependency;
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new CancellingTrackedQuery.Result();
        }
    }
}
