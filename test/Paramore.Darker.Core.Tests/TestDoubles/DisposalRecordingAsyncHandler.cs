using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class DisposalRecordingAsyncHandler
        : QueryHandlerAsync<DisposalRecordingQuery, DisposalRecordingResult>, IRecordsDisposal, IDisposable, IAsyncDisposable
    {
        public const string RESULT = "disposal-recording";

        private readonly DisposalRecord _record = new DisposalRecord();

        public string DisposedBy => _record.DisposedBy;

        [DisposalRecordingAsync(step: 1)]
        public override Task<DisposalRecordingResult> ExecuteAsync(DisposalRecordingQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new DisposalRecordingResult(RESULT));

        public void Dispose() => _record.Sync();

        public ValueTask DisposeAsync()
        {
            _record.Async();
            return default;
        }
    }
}
