namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler whose only constructor takes a <see cref="ScopedDependency"/>.</summary>
    public sealed class SecondCaptiveHandler : QueryHandler<SecondCaptiveQuery, SecondCaptiveResult>
    {
        public SecondCaptiveHandler(ScopedDependency dependency)
        {
        }

        public override SecondCaptiveResult Execute(SecondCaptiveQuery query) => new SecondCaptiveResult();
    }
}
