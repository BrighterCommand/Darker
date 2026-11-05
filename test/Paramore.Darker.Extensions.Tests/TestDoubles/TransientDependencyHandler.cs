namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler whose only constructor takes a <see cref="TransientDependency"/>.</summary>
    public sealed class TransientDependencyHandler : QueryHandler<CaptiveQuery, CaptiveResult>
    {
        public TransientDependencyHandler(TransientDependency dependency)
        {
        }

        public override CaptiveResult Execute(CaptiveQuery query) => new CaptiveResult();
    }
}
