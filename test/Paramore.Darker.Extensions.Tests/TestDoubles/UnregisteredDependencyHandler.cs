namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler whose only constructor takes an <see cref="UnregisteredDependency"/>.</summary>
    public sealed class UnregisteredDependencyHandler : QueryHandler<CaptiveQuery, CaptiveResult>
    {
        public UnregisteredDependencyHandler(UnregisteredDependency dependency)
        {
        }

        public override CaptiveResult Execute(CaptiveQuery query) => new CaptiveResult();
    }
}
