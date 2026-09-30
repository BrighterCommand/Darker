using Paramore.Darker.Policies;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class RetryableQueryHandler : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
#pragma warning disable CS0618 // Exercise legacy retry support retained until V6.
        [RetryableQuery(1, Constants.RetryPolicyName)]
#pragma warning restore CS0618
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
            => new SyncTestQuery.Result { Value = query.Id };
    }
}
