namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler whose only constructor takes a <see cref="ScopedDependency"/> then a <see cref="SecondScopedDependency"/>.</summary>
    public sealed class TwoScopedHandler : QueryHandler<TwoScopedQuery, TwoScopedResult>
    {
        public TwoScopedHandler(ScopedDependency first, SecondScopedDependency second)
        {
        }

        public override TwoScopedResult Execute(TwoScopedQuery query) => new TwoScopedResult();
    }
}
