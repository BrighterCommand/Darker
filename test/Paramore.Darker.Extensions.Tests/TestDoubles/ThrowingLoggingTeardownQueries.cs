using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    // Each query has a result type of its own, so the static per-TResult logger in the pipeline is
    // created against whichever logger factory is current when that query first runs.

    public sealed class LoggingFaultLifetimeQuery : IQuery<LoggingFaultLifetimeResult>
    {
    }

    public sealed class LoggingFaultLifetimeResult
    {
        public LoggingFaultLifetimeResult(string value) => Value = value;

        public string Value { get; }
    }

    /// <summary>A handler with a Scoped <see cref="ThrowingDisposeDependency"/>, so tearing the scope down throws.</summary>
    public sealed class LoggingFaultLifetimeQueryHandler : QueryHandler<LoggingFaultLifetimeQuery, LoggingFaultLifetimeResult>
    {
        public const string RESULT = "survived-lifetime-and-logging-faults";

        private readonly ThrowingDisposeDependency _dependency;

        public LoggingFaultLifetimeQueryHandler(ThrowingDisposeDependency dependency) => _dependency = dependency;

        public override LoggingFaultLifetimeResult Execute(LoggingFaultLifetimeQuery query)
            => new LoggingFaultLifetimeResult(_dependency != null ? RESULT : null);
    }

    public sealed class LoggingFaultReleaseQuery : IQuery<LoggingFaultReleaseResult>
    {
    }

    public sealed class LoggingFaultReleaseResult
    {
        public LoggingFaultReleaseResult(string value) => Value = value;

        public string Value { get; }
    }

    /// <summary>A handler whose <see cref="Dispose"/> throws, so releasing it makes the pipeline log a fault.</summary>
    public sealed class LoggingFaultReleaseQueryHandler : QueryHandler<LoggingFaultReleaseQuery, LoggingFaultReleaseResult>, IDisposable
    {
        public const string RESULT = "survived-release-and-logging-faults";

        public override LoggingFaultReleaseResult Execute(LoggingFaultReleaseQuery query)
            => new LoggingFaultReleaseResult(RESULT);

        public void Dispose() => throw new InvalidOperationException("handler failed to dispose");
    }
}
