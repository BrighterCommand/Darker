using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A distinctive exception, so a test can pick the dependency's fault out of the captured logs.</summary>
    public sealed class DependencyDisposeFaultException : Exception
    {
        public DependencyDisposeFaultException() : base("dependency failed to dispose") { }
    }

    /// <summary>A service whose <see cref="Dispose"/> throws, with its own query so the pipeline's closed generic is disjoint.</summary>
    public sealed class ThrowingDisposeDependency : IDisposable
    {
        public void Dispose() => throw new DependencyDisposeFaultException();
    }

    public sealed class ThrowingDisposeDependencyQuery : IQuery<ThrowingDisposeDependencyResult>
    {
    }

    public sealed class ThrowingDisposeDependencyResult
    {
        public ThrowingDisposeDependencyResult(string value) => Value = value;

        public string Value { get; }
    }

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
