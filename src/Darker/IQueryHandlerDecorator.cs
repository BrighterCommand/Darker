using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darker
{
    public interface IQueryHandlerDecorator
    {
        IQueryContext Context { get; set; }
        void InitializeFromAttributeParams(object[] attributeParams);
    }

    public interface IQueryHandlerDecorator<TQuery, TResult> : IQueryHandlerDecorator
        where TQuery : IQuery<TResult>
    {
        TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback);

        Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}