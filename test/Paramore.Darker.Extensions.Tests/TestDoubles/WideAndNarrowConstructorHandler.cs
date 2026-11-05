namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler with two public constructors, the narrower declared first.</summary>
    public sealed class WideAndNarrowConstructorHandler : QueryHandler<CaptiveQuery, CaptiveResult>
    {
        public WideAndNarrowConstructorHandler(OtherDependency other)
        {
        }

        public WideAndNarrowConstructorHandler(OtherDependency other, ScopedDependency dependency)
        {
        }

        public override CaptiveResult Execute(CaptiveQuery query) => new CaptiveResult();
    }
}
