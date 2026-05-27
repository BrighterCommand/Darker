using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Policies;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Tests.TestDoubles
{
    internal class AsyncRetryableQueryHandler : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        [RetryableQueryAttributeAsync(1, Constants.RetryPolicyName)]
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
    }
}
