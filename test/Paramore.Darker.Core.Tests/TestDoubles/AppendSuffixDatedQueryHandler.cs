namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class AppendSuffixDatedQueryHandler : QueryHandler<DatedQuery, string>
    {
        [AppendSuffix(step: 1)]
        public override string Execute(DatedQuery query) => "routed";
    }
}
