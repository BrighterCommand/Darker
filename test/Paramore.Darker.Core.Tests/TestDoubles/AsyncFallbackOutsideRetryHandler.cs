using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Policies;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>An async handler whose Fallback (Step 1) is outside its Retry (Step 2), the correct order.</summary>
    internal class AsyncFallbackOutsideRetryHandler : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        [FallbackPolicyAttributeAsync(1)]
        [RetryableQueryAttributeAsync(2, Constants.RetryPolicyName)]
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("fails");

        public override Task<AsyncTestQuery.Result> FallbackAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
    }
}
