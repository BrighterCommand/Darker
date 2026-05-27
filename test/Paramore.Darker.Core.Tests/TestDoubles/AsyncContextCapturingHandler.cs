using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class AsyncContextCapturingHandler : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        public IQueryContext CapturedContext { get; private set; }

        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
        {
            CapturedContext = Context;
            return Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
        }
    }
}
