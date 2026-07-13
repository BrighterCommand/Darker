using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker
{
    public interface IQueryProcessor
    {
        TResult Execute<TResult>(IQuery<TResult> query, IQueryContext queryContext = null);

        Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, IQueryContext queryContext = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Executes a stream query, yielding results lazily as an async sequence.
        /// Span and handler lifetime are bound to enumeration — release on enumerator disposal.
        /// </summary>
        IAsyncEnumerable<TResult> ExecuteStream<TResult>(IStreamQuery<TResult> query, IQueryContext queryContext = null, CancellationToken cancellationToken = default);
    }
}