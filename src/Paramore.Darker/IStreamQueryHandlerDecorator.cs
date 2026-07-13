using System;
using System.Collections.Generic;
using System.Threading;

namespace Paramore.Darker
{
    /// <summary>
    /// Decorator that wraps a stream query handler, allowing cross-cutting concerns over the async stream.
    /// </summary>
    public interface IStreamQueryHandlerDecorator<TQuery, TResult> : IQueryHandlerDecorator
        where TQuery : IStreamQuery<TResult>
    {
        IAsyncEnumerable<TResult> Execute(
            TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            CancellationToken cancellationToken);
    }
}
