namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class NewDatedQueryHandler : QueryHandler<DatedQuery, string>
    {
        public override string Execute(DatedQuery query) => "new";
    }
}
