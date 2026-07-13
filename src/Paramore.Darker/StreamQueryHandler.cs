using System.Collections.Generic;
using System.Threading;

namespace Paramore.Darker
{
    /// <summary>
    /// Abstract base class for stream query handlers.
    /// No Fallback method — a partially-emitted stream has no meaningful fallback value.
    /// </summary>
    public abstract class StreamQueryHandler<TQuery, TResult> : IStreamQueryHandler<TQuery, TResult>
        where TQuery : IStreamQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public abstract IAsyncEnumerable<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default);
    }
}
