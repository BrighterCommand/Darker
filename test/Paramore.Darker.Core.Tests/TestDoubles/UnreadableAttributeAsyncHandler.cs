using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An async handler whose ExecuteAsync carries an attribute that cannot be constructed.
    /// </summary>
    internal class UnreadableAttributeAsyncHandler : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        [ThrowingConstructor]
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
        }
    }
}
