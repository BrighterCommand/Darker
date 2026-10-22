namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class PartialBuildHandler : QueryHandler<PartialBuildQuery, string>
    {
        [FirstPassThrough(step: 2)]
        [ThrowingInit(step: 1)]
        public override string Execute(PartialBuildQuery query) => "never-reached";
    }
}
