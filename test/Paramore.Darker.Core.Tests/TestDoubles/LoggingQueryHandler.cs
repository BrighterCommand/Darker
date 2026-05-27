using Paramore.Darker.Logging.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class LoggingQueryHandler : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [QueryLogging(1)]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
            => new SyncTestQuery.Result { Value = query.Id };
    }
}
