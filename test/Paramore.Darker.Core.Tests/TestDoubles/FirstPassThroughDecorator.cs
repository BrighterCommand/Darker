using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A decorator that calls the next step unchanged.</summary>
    internal sealed class FirstPassThroughDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback) => next(query);
    }
}
