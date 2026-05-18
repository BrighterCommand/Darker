using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker
{
    public interface IQueryHandlerDecoratorAsync<TQuery, TResult> : IQueryHandlerDecorator
        where TQuery : IQuery<TResult>
    {
        Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
