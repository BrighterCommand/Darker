using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An async handler with two public ExecuteAsync overloads, so the path's method is ambiguous.
    /// </summary>
    internal class OverloadedExecuteAsyncHandler : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
        }

        public Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query, int attempts,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(query, cancellationToken);
        }
    }
}
