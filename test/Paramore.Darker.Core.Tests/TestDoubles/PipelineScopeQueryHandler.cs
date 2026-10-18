namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class PipelineScopeQueryHandler : QueryHandler<PipelineScopeQuery, string>
    {
        public const string RESULT = "pipeline-scope-sync";

        [FirstPassThrough(step: 1)]
        public override string Execute(PipelineScopeQuery query) => RESULT;
    }
}
