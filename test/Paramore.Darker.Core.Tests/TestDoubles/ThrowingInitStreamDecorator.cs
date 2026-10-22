using System;
using System.Collections.Generic;
using System.Threading;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class ThrowingInitStreamDecorator<TQuery, TResult> : IStreamQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IStreamQuery<TResult>
    {
        private readonly Exception _fault;

        public ThrowingInitStreamDecorator(Exception fault) => _fault = fault;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) => throw _fault;

        public IAsyncEnumerable<TResult> Execute(TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            CancellationToken cancellationToken)
            => next(query, cancellationToken);
    }
}
