namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class LifetimeIsolationQueryHandler : QueryHandler<LifetimeIsolationQuery, LifetimeIsolationResult>
    {
        public const string RESULT = "isolated";

        public override LifetimeIsolationResult Execute(LifetimeIsolationQuery query)
            => new LifetimeIsolationResult(RESULT);
    }
}
