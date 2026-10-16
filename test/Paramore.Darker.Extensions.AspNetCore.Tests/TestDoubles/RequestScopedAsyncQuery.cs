using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.AspNetCore.Tests.TestDoubles
{
    /// <summary>An asynchronous query whose handler returns the dependency it was constructed with.</summary>
    public sealed class RequestScopedAsyncQuery : IQuery<RequestScopedDependency>
    {
    }

    public sealed class RequestScopedAsyncQueryHandler : QueryHandlerAsync<RequestScopedAsyncQuery, RequestScopedDependency>
    {
        private readonly RequestScopedDependency _dependency;

        public RequestScopedAsyncQueryHandler(RequestScopedDependency dependency) => _dependency = dependency;

        public override Task<RequestScopedDependency> ExecuteAsync(
            RequestScopedAsyncQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(_dependency);
    }
}
