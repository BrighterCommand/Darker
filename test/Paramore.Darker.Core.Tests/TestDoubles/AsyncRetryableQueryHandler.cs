using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Policies;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class AsyncRetryableQueryHandler : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
#pragma warning disable CS0618 // Exercise legacy retry support retained until V6.
        [RetryableQueryAttributeAsync(1, Constants.RetryPolicyName)]
#pragma warning restore CS0618
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
    }
}
