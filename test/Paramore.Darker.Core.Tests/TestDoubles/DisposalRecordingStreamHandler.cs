using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class DisposalRecordingStreamHandler
        : IStreamQueryHandler<DisposalRecordingStreamQuery, DisposalRecordingStreamItem>, IRecordsDisposal, IDisposable, IAsyncDisposable
    {
        public const string ITEM = "disposal-recording-item";

        private readonly DisposalRecord _record = new DisposalRecord();

        public IQueryContext Context { get; set; }

        public string DisposedBy => _record.DisposedBy;

        [DisposalRecordingStream(step: 1)]
        public async IAsyncEnumerable<DisposalRecordingStreamItem> ExecuteAsync(
            DisposalRecordingStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new DisposalRecordingStreamItem(ITEM);
        }

        public void Dispose() => _record.Sync();

        public ValueTask DisposeAsync()
        {
            _record.Async();
            return default;
        }
    }
}
