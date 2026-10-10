namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class LifetimeIsolationQuery : IQuery<LifetimeIsolationResult>
    {
    }

    internal sealed class LifetimeIsolationResult
    {
        public LifetimeIsolationResult(string value) => Value = value;

        public string Value { get; }
    }

    internal sealed class LifetimeIsolationQueryHandler : QueryHandler<LifetimeIsolationQuery, LifetimeIsolationResult>
    {
        public const string RESULT = "isolated";

        public override LifetimeIsolationResult Execute(LifetimeIsolationQuery query)
            => new LifetimeIsolationResult(RESULT);
    }
}
