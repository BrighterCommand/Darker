using System;
using System.Diagnostics;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A decorator that records <see cref="Activity.Current"/> once the rest of the pipeline has returned to it.</summary>
    internal sealed class CurrentActivityCapturingDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public Activity CurrentAfterNext { get; private set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            try
            {
                return next(query);
            }
            finally
            {
                CurrentAfterNext = Activity.Current;
            }
        }
    }
}
