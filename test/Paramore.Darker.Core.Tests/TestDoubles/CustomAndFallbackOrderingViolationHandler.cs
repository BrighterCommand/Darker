using System;
using Paramore.Darker.Policies;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A sync handler that violates two ordering rules: its Retry (Step 1) is outside its Fallback (Step 2),
    /// and its Inner double (Step 3) is outside its Outer double (Step 4).
    /// </summary>
    internal class CustomAndFallbackOrderingViolationHandler : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [RetryableQuery(1, Constants.RetryPolicyName)]
        [FallbackPolicy(2)]
        [InnerDouble(3)]
        [OuterDouble(4)]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
            => throw new InvalidOperationException("fails");

        public override SyncTestQuery.Result Fallback(SyncTestQuery query)
            => new SyncTestQuery.Result { Value = query.Id };
    }
}
