namespace Paramore.Darker.Extensions.AspNetCore.Tests.TestDoubles
{
    /// <summary>A synchronous query whose handler returns the dependency it was constructed with.</summary>
    public sealed class RequestScopedSyncQuery : IQuery<RequestScopedDependency>
    {
    }

    public sealed class RequestScopedSyncQueryHandler : QueryHandler<RequestScopedSyncQuery, RequestScopedDependency>
    {
        private readonly RequestScopedDependency _dependency;

        public RequestScopedSyncQueryHandler(RequestScopedDependency dependency) => _dependency = dependency;

        public override RequestScopedDependency Execute(RequestScopedSyncQuery query) => _dependency;
    }
}
