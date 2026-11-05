namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler with two one-parameter public constructors, declared in the order named by the type.</summary>
    public sealed class EqualWidthOtherFirstConstructorHandler : QueryHandler<CaptiveQuery, CaptiveResult>
    {
        public EqualWidthOtherFirstConstructorHandler(OtherDependency other)
        {
        }

        public EqualWidthOtherFirstConstructorHandler(ScopedDependency dependency)
        {
        }

        public override CaptiveResult Execute(CaptiveQuery query) => new CaptiveResult();
    }
}
