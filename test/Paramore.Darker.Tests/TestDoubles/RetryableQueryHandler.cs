using Paramore.Darker.Policies;

namespace Paramore.Darker.Tests.TestDoubles
{
    public class RetryableQueryHandler : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [RetryableQuery(1, Constants.RetryPolicyName)]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
            => new SyncTestQuery.Result { Value = query.Id };
    }
}
