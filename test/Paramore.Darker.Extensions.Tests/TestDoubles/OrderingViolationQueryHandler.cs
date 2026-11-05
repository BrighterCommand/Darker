using Paramore.Darker.Policies;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A public handler that violates two ordering rules: its Retry (Step 1) is outside its Fallback (Step 2),
    /// and its Inner double (Step 3) is outside its Outer double (Step 4).
    /// </summary>
    public sealed class OrderingViolationQueryHandler : QueryHandler<OrderingViolationQuery, OrderingViolationResult>
    {
        [RetryableQuery(1, Constants.RetryPolicyName)]
        [FallbackPolicy(2)]
        [InnerDouble(3)]
        [OuterDouble(4)]
        public override OrderingViolationResult Execute(OrderingViolationQuery query) => new OrderingViolationResult();
    }
}
