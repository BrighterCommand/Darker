using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A pass-through decorator used as one half of a caller-supplied ordering rule.</summary>
    public sealed class InnerDouble<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback) => next(query);
    }
}
