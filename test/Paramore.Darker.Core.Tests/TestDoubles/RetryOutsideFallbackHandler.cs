using System;
using Paramore.Darker.Policies;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A sync handler whose Retry (Step 1) is outside its Fallback (Step 2), so the Fallback hides the failure from the Retry.</summary>
    internal class RetryOutsideFallbackHandler : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [RetryableQuery(1, Constants.RetryPolicyName)]
        [FallbackPolicy(2)]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
            => throw new InvalidOperationException("fails");

        public override SyncTestQuery.Result Fallback(SyncTestQuery query)
            => new SyncTestQuery.Result { Value = query.Id };
    }
}
