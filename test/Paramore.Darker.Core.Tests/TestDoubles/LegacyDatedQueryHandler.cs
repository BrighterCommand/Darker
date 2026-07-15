namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class LegacyDatedQueryHandler : QueryHandler<DatedQuery, string>
    {
        public override string Execute(DatedQuery query) => "legacy";
    }
}
