using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An async handler whose ExecuteAsync carries an attribute naming a decorator that closes over a concrete query class only.
    /// </summary>
    internal class QueryConstructingAsyncHandler : QueryHandlerAsync<TestQuery, TestQuery.Result>
    {
        [QueryConstructingAsync]
        public override Task<TestQuery.Result> ExecuteAsync(TestQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestQuery.Result());
    }
}
