using System;
using System.Collections.Generic;
using System.Threading;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class PassThroughStreamDecorator<TQuery, TResult> : IStreamQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IStreamQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public IAsyncEnumerable<TResult> Execute(TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            CancellationToken cancellationToken)
            => next(query, cancellationToken);
    }
}
