using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class DisposalRecordingAsyncDecorator<TQuery, TResult>
        : IQueryHandlerDecoratorAsync<TQuery, TResult>, IRecordsDisposal, IDisposable, IAsyncDisposable
        where TQuery : IQuery<TResult>
    {
        private readonly DisposalRecord _record = new DisposalRecord();

        public IQueryContext Context { get; set; }

        public string DisposedBy => _record.DisposedBy;

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default)
            => next(query, cancellationToken);

        public void Dispose() => _record.Sync();

        public ValueTask DisposeAsync()
        {
            _record.Async();
            return default;
        }
    }
}
