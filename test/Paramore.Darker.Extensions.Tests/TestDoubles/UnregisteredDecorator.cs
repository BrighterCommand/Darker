using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A decorator that no test registers with the container.</summary>
    public sealed class UnregisteredDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback) => next(query);
    }
}
