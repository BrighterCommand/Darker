using System;
using Paramore.Darker.Policies;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A sync handler whose Fallback (Step 1) is outside its Retry (Step 2), the correct order.</summary>
    internal class FallbackOutsideRetryHandler : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [FallbackPolicy(1)]
        [RetryableQuery(2, Constants.RetryPolicyName)]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
            => throw new InvalidOperationException("fails");

        public override SyncTestQuery.Result Fallback(SyncTestQuery query)
            => new SyncTestQuery.Result { Value = query.Id };
    }
}
