using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class FaultOnDisposeAsyncDecorator<TQuery, TResult>
        : IQueryHandlerDecoratorAsync<TQuery, TResult>, IRecordsAsyncDisposal, IAsyncDisposable
        where TQuery : IQuery<TResult>
    {
        private readonly FaultingAsyncDisposal _disposal;

        public FaultOnDisposeAsyncDecorator(Exception fault) => _disposal = new FaultingAsyncDisposal(fault);

        public IQueryContext Context { get; set; }

        public int DisposeAsyncCount => _disposal.Count;

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default)
            => next(query, cancellationToken);

        public ValueTask DisposeAsync() => _disposal.DisposeAsync();
    }
}
