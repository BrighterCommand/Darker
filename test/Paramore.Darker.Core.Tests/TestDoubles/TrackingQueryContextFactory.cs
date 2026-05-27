namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class TrackingQueryContextFactory : IQueryContextFactory
    {
        public int CreateCallCount { get; private set; }

        public IQueryContext Create()
        {
            CreateCallCount++;
            return new QueryContext { Bag = new System.Collections.Generic.Dictionary<string, object>() };
        }

        public void Release(IQueryContext queryContext)
        {
        }
    }
}
