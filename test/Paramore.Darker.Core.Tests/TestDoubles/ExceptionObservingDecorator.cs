using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A decorator that records the exception the rest of the pipeline throws to it, then rethrows it.</summary>
    internal sealed class ExceptionObservingDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public Exception Observed { get; private set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            try
            {
                return next(query);
            }
            catch (Exception ex)
            {
                Observed = ex;
                throw;
            }
        }
    }
}
