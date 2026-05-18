using System;

namespace Paramore.Darker
{
    public interface IQueryHandlerDecorator
    {
        IQueryContext Context { get; set; }

        void InitializeFromAttributeParams(object[] attributeParams);
    }

    public interface IQueryHandlerDecorator<TQuery, TResult> : IQueryHandlerDecorator
        where TQuery : IQuery<TResult>
    {
        TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback);
    }
}