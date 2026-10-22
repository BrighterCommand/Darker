using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class LoggingFaultStreamLifetimeHandler
        : StreamQueryHandler<LoggingFaultStreamQuery<LifetimeFaultTag>, LoggingFaultStreamItem<LifetimeFaultTag>>
    {
        public const string ITEM = "survived-stream-lifetime-and-logging-faults";

        private readonly FaultingAsyncDisposeDependency _dependency;

        public LoggingFaultStreamLifetimeHandler(FaultingAsyncDisposeDependency dependency) => _dependency = dependency;

        public override async IAsyncEnumerable<LoggingFaultStreamItem<LifetimeFaultTag>> ExecuteAsync(
            LoggingFaultStreamQuery<LifetimeFaultTag> query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new LoggingFaultStreamItem<LifetimeFaultTag>(_dependency != null ? ITEM : null);
            yield return new LoggingFaultStreamItem<LifetimeFaultTag>(ITEM);
        }
    }
}
