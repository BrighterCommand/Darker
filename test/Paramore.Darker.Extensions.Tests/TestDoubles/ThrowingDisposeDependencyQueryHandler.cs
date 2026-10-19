namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class ThrowingDisposeDependencyQueryHandler
        : QueryHandler<ThrowingDisposeDependencyQuery, ThrowingDisposeDependencyResult>
    {
        public const string RESULT = "survived-dependency-dispose-fault";

        private readonly ThrowingDisposeDependency _dependency;

        public ThrowingDisposeDependencyQueryHandler(ThrowingDisposeDependency dependency) => _dependency = dependency;

        public override ThrowingDisposeDependencyResult Execute(ThrowingDisposeDependencyQuery query)
            => new ThrowingDisposeDependencyResult(_dependency != null ? RESULT : null);
    }
}
