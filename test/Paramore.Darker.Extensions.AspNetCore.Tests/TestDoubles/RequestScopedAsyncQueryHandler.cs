using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.AspNetCore.Tests.TestDoubles
{
    public sealed class RequestScopedAsyncQueryHandler : QueryHandlerAsync<RequestScopedAsyncQuery, RequestScopedDependency>
    {
        private readonly RequestScopedDependency _dependency;

        public RequestScopedAsyncQueryHandler(RequestScopedDependency dependency) => _dependency = dependency;

        public override Task<RequestScopedDependency> ExecuteAsync(
            RequestScopedAsyncQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(_dependency);
    }
}
