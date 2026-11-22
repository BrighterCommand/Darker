using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>An async decorator that records the exception the rest of the pipeline throws to it, then rethrows it.</summary>
    internal sealed class ExceptionObservingAsyncDecorator<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public Exception Observed { get; private set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public async Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await next(query, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Observed = ex;
                throw;
            }
        }
    }
}
