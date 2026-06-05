using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// Handler for <see cref="ProcessorQuery"/> used by the sync <c>ExecutesQueries</c>
    /// test: it both writes <c>"id" =&gt; query.Id</c> to the context bag and returns
    /// <c>query.Id</c>. Because it needs access to <see cref="QueryHandler{TQuery,TResult}.Context"/>
    /// to write the bag, it is a dedicated double rather than the delegate-only
    /// <see cref="RecordingQueryHandler{TQuery,TResult}"/> (ADR 0013, Decision 3).
    /// </summary>
    internal class ProcessorQueryHandler : QueryHandler<ProcessorQuery, Guid>
    {
        public override Guid Execute(ProcessorQuery query)
        {
            Context.Bag.Add("id", query.Id);
            return query.Id;
        }
    }
}
