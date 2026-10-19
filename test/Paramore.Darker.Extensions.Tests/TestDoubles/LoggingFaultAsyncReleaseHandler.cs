using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A handler whose <see cref="DisposeAsync"/> faults, so releasing it makes the pipeline log a fault.</summary>
    public sealed class LoggingFaultAsyncReleaseHandler
        : QueryHandlerAsync<LoggingFaultAsyncQuery<ReleaseFaultTag>, LoggingFaultAsyncResult<ReleaseFaultTag>>, IAsyncDisposable
    {
        public const string RESULT = "survived-async-release-and-logging-faults";

        public override Task<LoggingFaultAsyncResult<ReleaseFaultTag>> ExecuteAsync(
            LoggingFaultAsyncQuery<ReleaseFaultTag> query, CancellationToken cancellationToken = default)
            => Task.FromResult(new LoggingFaultAsyncResult<ReleaseFaultTag>(RESULT));

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("handler failed to dispose asynchronously");
        }
    }
}
