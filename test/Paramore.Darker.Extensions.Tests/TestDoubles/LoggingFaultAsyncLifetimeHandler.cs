using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A handler with a Scoped <see cref="FaultingAsyncDisposeDependency"/>, so tearing the scope down faults.</summary>
    public sealed class LoggingFaultAsyncLifetimeHandler
        : QueryHandlerAsync<LoggingFaultAsyncQuery<LifetimeFaultTag>, LoggingFaultAsyncResult<LifetimeFaultTag>>
    {
        public const string RESULT = "survived-async-lifetime-and-logging-faults";

        private readonly FaultingAsyncDisposeDependency _dependency;

        public LoggingFaultAsyncLifetimeHandler(FaultingAsyncDisposeDependency dependency) => _dependency = dependency;

        public override Task<LoggingFaultAsyncResult<LifetimeFaultTag>> ExecuteAsync(
            LoggingFaultAsyncQuery<LifetimeFaultTag> query, CancellationToken cancellationToken = default)
            => Task.FromResult(new LoggingFaultAsyncResult<LifetimeFaultTag>(_dependency != null ? RESULT : null));
    }
}
