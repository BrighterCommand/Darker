namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class ContextCapturingHandler : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        public IQueryContext CapturedContext { get; private set; }

        public override SyncTestQuery.Result Execute(SyncTestQuery query)
        {
            CapturedContext = Context;
            return new SyncTestQuery.Result { Value = query.Id };
        }
    }
}
