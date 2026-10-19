namespace Paramore.Darker.Extensions.AspNetCore.Tests.TestDoubles
{
    public sealed class RequestScopedSyncQueryHandler : QueryHandler<RequestScopedSyncQuery, RequestScopedDependency>
    {
        private readonly RequestScopedDependency _dependency;

        public RequestScopedSyncQueryHandler(RequestScopedDependency dependency) => _dependency = dependency;

        public override RequestScopedDependency Execute(RequestScopedSyncQuery query) => _dependency;
    }
}
