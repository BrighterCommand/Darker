using System;
using System.Collections.Generic;
using System.Threading;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class TrackedStreamDecorator<TQuery, TResult> : IStreamQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IStreamQuery<TResult>
    {
        private readonly ITrackedDependency _dependency;

        public TrackedStreamDecorator(ITrackedDependency dependency) => _dependency = dependency;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public IAsyncEnumerable<TResult> Execute(
            TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            CancellationToken cancellationToken)
        {
            if (query is TrackedStreamQuery tracked)
                tracked.DecoratorDependency = _dependency;
            return next(query, cancellationToken);
        }
    }
}
