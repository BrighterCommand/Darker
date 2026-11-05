namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler whose only constructor takes a <see cref="SingletonDependency"/>.</summary>
    public sealed class SingletonDependencyHandler : QueryHandler<CaptiveQuery, CaptiveResult>
    {
        public SingletonDependencyHandler(SingletonDependency dependency)
        {
        }

        public override CaptiveResult Execute(CaptiveQuery query) => new CaptiveResult();
    }
}
