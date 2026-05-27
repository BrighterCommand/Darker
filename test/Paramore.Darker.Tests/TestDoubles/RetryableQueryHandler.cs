using Paramore.Darker.Policies;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Tests.TestDoubles
{
    internal class RetryableQueryHandler : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [RetryableQuery(1, Constants.RetryPolicyName)]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
            => new SyncTestQuery.Result { Value = query.Id };
    }
}
