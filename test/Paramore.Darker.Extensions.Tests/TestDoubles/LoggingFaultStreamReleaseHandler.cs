using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class LoggingFaultStreamReleaseHandler
        : IStreamQueryHandler<LoggingFaultStreamQuery<ReleaseFaultTag>, LoggingFaultStreamItem<ReleaseFaultTag>>, IAsyncDisposable
    {
        public const string ITEM = "survived-stream-release-and-logging-faults";

        public IQueryContext Context { get; set; }

        public async IAsyncEnumerable<LoggingFaultStreamItem<ReleaseFaultTag>> ExecuteAsync(
            LoggingFaultStreamQuery<ReleaseFaultTag> query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new LoggingFaultStreamItem<ReleaseFaultTag>(ITEM);
            yield return new LoggingFaultStreamItem<ReleaseFaultTag>(ITEM);
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("stream handler failed to dispose asynchronously");
        }
    }
}
