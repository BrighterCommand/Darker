namespace Paramore.Darker.Benchmarks
{
    public class BasicSyncQuery : IQuery<bool>
    {
    }

    public class BasicSyncQueryHandler : QueryHandler<BasicSyncQuery, bool>
    {
        public override bool Execute(BasicSyncQuery query)
        {
            return true;
        }
    }
}