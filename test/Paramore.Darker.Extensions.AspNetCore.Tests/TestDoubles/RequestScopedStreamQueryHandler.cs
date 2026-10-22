using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.AspNetCore.Tests.TestDoubles
{
    public sealed class RequestScopedStreamQueryHandler : StreamQueryHandler<RequestScopedStreamQuery, RequestScopedDependency>
    {
        private readonly RequestScopedDependency _dependency;

        public RequestScopedStreamQueryHandler(RequestScopedDependency dependency) => _dependency = dependency;

        public override async IAsyncEnumerable<RequestScopedDependency> ExecuteAsync(
            RequestScopedStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return _dependency;
        }
    }
}
