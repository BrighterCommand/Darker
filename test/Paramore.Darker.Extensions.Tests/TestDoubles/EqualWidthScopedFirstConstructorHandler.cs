namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler with two one-parameter public constructors, declared in the order named by the type.</summary>
    public sealed class EqualWidthScopedFirstConstructorHandler : QueryHandler<CaptiveQuery, CaptiveResult>
    {
        public EqualWidthScopedFirstConstructorHandler(ScopedDependency dependency)
        {
        }

        public EqualWidthScopedFirstConstructorHandler(OtherDependency other)
        {
        }

        public override CaptiveResult Execute(CaptiveQuery query) => new CaptiveResult();
    }
}
