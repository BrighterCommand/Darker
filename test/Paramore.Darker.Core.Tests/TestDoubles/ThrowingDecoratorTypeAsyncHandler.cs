using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An async handler whose ExecuteAsync carries an attribute whose GetDecoratorType throws.
    /// </summary>
    internal class ThrowingDecoratorTypeAsyncHandler : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        [ThrowingDecoratorType]
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
        }
    }
}
