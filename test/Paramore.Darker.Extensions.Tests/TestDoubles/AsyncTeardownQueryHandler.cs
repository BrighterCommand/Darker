using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class AsyncTeardownQueryHandler : QueryHandlerAsync<AsyncTeardownQuery, AsyncTeardownResult>
    {
        public const string RESULT = "async-teardown";

        private readonly YieldingAsyncOnlyDisposableDependency _dependency;

        public AsyncTeardownQueryHandler(YieldingAsyncOnlyDisposableDependency dependency) => _dependency = dependency;

        public override Task<AsyncTeardownResult> ExecuteAsync(AsyncTeardownQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new AsyncTeardownResult(_dependency != null ? RESULT : null));
    }
}
