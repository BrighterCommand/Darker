using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// Async sibling of <see cref="ProcessorQueryHandler"/> for the async
    /// <c>ExecutesQueries</c> test: writes <c>"id" =&gt; query.Id</c> to the context bag
    /// and returns <c>query.Id</c>. A dedicated double because it needs the
    /// <see cref="QueryHandlerAsync{TQuery,TResult}.Context"/> the delegate-only
    /// recording double cannot reach (ADR 0013, Decision 3).
    /// </summary>
    internal class ProcessorQueryHandlerAsync : QueryHandlerAsync<ProcessorQuery, Guid>
    {
        public override Task<Guid> ExecuteAsync(ProcessorQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            Context.Bag.Add("id", query.Id);
            return Task.FromResult(query.Id);
        }
    }
}
