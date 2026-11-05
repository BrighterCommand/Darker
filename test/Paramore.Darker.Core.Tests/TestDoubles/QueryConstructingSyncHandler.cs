namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A sync handler whose Execute carries an attribute naming a decorator that closes over a concrete query class only.
    /// </summary>
    internal class QueryConstructingSyncHandler : QueryHandler<TestQuery, TestQuery.Result>
    {
        [QueryConstructingSync]
        public override TestQuery.Result Execute(TestQuery query) => new();
    }
}
