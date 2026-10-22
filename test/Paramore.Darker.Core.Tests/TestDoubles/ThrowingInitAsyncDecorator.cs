using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class ThrowingInitAsyncDecorator<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly Exception _fault;

        public ThrowingInitAsyncDecorator(Exception fault) => _fault = fault;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) => throw _fault;

        public Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default)
            => next(query, cancellationToken);
    }
}
