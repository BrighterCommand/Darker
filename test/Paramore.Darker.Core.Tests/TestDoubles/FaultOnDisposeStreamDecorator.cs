using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class FaultOnDisposeStreamDecorator<TQuery, TResult>
        : IStreamQueryHandlerDecorator<TQuery, TResult>, IRecordsAsyncDisposal, IAsyncDisposable
        where TQuery : IStreamQuery<TResult>
    {
        private readonly FaultingAsyncDisposal _disposal;

        public FaultOnDisposeStreamDecorator(Exception fault) => _disposal = new FaultingAsyncDisposal(fault);

        public IQueryContext Context { get; set; }

        public int DisposeAsyncCount => _disposal.Count;

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public IAsyncEnumerable<TResult> Execute(TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            CancellationToken cancellationToken)
            => next(query, cancellationToken);

        public ValueTask DisposeAsync() => _disposal.DisposeAsync();
    }
}
