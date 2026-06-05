using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class TestExceptionDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public IQueryContext Context { get; set; }
        public void InitializeFromAttributeParams(object[] attributeParams) { }
        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            throw new InvalidOperationException("Test exception from decorator");
        }
    }
}
