using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Policies;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>An async handler whose Retry (Step 1) is outside its Fallback (Step 2), so the Fallback hides the failure from the Retry.</summary>
    internal class AsyncRetryOutsideFallbackHandler : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        [RetryableQueryAttributeAsync(1, Constants.RetryPolicyName)]
        [FallbackPolicyAttributeAsync(2)]
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("fails");

        public override Task<AsyncTestQuery.Result> FallbackAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
    }
}
