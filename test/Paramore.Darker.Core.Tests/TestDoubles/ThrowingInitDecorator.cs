using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class ThrowingInitDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly Exception _fault;

        public ThrowingInitDecorator(Exception fault) => _fault = fault;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) => throw _fault;

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback) => next(query);
    }
}
