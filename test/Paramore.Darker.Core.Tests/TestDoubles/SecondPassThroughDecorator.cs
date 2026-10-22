using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A second, distinct pass-through decorator, so a handler can carry two decorators.</summary>
    internal sealed class SecondPassThroughDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback) => next(query);
    }
}
