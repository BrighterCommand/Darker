using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A handler whose <see cref="Dispose"/> throws, so releasing it makes the pipeline log a fault.</summary>
    public sealed class LoggingFaultReleaseQueryHandler : QueryHandler<LoggingFaultReleaseQuery, LoggingFaultReleaseResult>, IDisposable
    {
        public const string RESULT = "survived-release-and-logging-faults";

        public override LoggingFaultReleaseResult Execute(LoggingFaultReleaseQuery query)
            => new LoggingFaultReleaseResult(RESULT);

        public void Dispose() => throw new InvalidOperationException("handler failed to dispose");
    }
}
