namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class ThrowingScopeQueryHandler : QueryHandler<ThrowingScopeQuery, ThrowingScopeResult>
    {
        public const string RESULT = "throwing-scope-sync";

        public override ThrowingScopeResult Execute(ThrowingScopeQuery query) => new ThrowingScopeResult(RESULT);
    }
}
