using Paramore.Darker.Logging.Attributes;

namespace Paramore.Darker.Tests.TestDoubles
{
    public class LoggingQueryHandler : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [QueryLogging(1)]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
            => new SyncTestQuery.Result { Value = query.Id };
    }
}
