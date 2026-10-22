using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class DisposalRecordingStreamDecorator<TQuery, TResult>
        : IStreamQueryHandlerDecorator<TQuery, TResult>, IRecordsDisposal, IDisposable, IAsyncDisposable
        where TQuery : IStreamQuery<TResult>
    {
        private readonly DisposalRecord _record = new DisposalRecord();

        public IQueryContext Context { get; set; }

        public string DisposedBy => _record.DisposedBy;

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public IAsyncEnumerable<TResult> Execute(TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            CancellationToken cancellationToken)
            => next(query, cancellationToken);

        public void Dispose() => _record.Sync();

        public ValueTask DisposeAsync()
        {
            _record.Async();
            return default;
        }
    }
}
