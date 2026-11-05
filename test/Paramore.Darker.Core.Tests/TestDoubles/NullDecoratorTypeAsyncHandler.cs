using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An async handler whose ExecuteAsync carries an attribute with a null decorator type.
    /// </summary>
    internal class NullDecoratorTypeAsyncHandler : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        [NullDecoratorType]
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
        }
    }
}
