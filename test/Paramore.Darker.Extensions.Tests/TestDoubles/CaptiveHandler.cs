namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler whose only constructor takes a <see cref="ScopedDependency"/>.</summary>
    public sealed class CaptiveHandler : QueryHandler<CaptiveQuery, CaptiveResult>
    {
        public CaptiveHandler(ScopedDependency dependency)
        {
        }

        public override CaptiveResult Execute(CaptiveQuery query) => new CaptiveResult();
    }
}
