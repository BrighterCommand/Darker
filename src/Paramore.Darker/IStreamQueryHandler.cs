using System.Collections.Generic;
using System.Threading;

namespace Paramore.Darker
{
    /// <summary>
    /// Handler that processes a stream query and yields results incrementally.
    /// </summary>
    public interface IStreamQueryHandler<in TQuery, TResult> : IQueryHandler
        where TQuery : IStreamQuery<TResult>
    {
        IAsyncEnumerable<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default);
    }
}
