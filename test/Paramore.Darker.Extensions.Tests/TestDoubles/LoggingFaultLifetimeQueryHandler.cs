namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A handler with a Scoped <see cref="ThrowingDisposeDependency"/>, so tearing the scope down throws.</summary>
    public sealed class LoggingFaultLifetimeQueryHandler : QueryHandler<LoggingFaultLifetimeQuery, LoggingFaultLifetimeResult>
    {
        public const string RESULT = "survived-lifetime-and-logging-faults";

        private readonly ThrowingDisposeDependency _dependency;

        public LoggingFaultLifetimeQueryHandler(ThrowingDisposeDependency dependency) => _dependency = dependency;

        public override LoggingFaultLifetimeResult Execute(LoggingFaultLifetimeQuery query)
            => new LoggingFaultLifetimeResult(_dependency != null ? RESULT : null);
    }
}
