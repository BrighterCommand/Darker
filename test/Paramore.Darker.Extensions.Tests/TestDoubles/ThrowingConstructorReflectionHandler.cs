namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler whose constructor parameter carries an attribute that throws when read.</summary>
    public sealed class ThrowingConstructorReflectionHandler : QueryHandler<CaptiveQuery, CaptiveResult>
    {
        public ThrowingConstructorReflectionHandler([ThrowingKeyed] ScopedDependency dependency)
        {
        }

        public override CaptiveResult Execute(CaptiveQuery query) => new CaptiveResult();
    }
}
