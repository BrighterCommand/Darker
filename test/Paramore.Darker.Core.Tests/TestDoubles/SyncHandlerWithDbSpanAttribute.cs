using Paramore.Darker.Observability;
using Paramore.Darker.Observability.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A minimal sync handler decorated with <see cref="QueryDbSpanAttribute"/> used to verify
    /// that the pipeline weaves a <c>QueryDbSpanDecorator</c> child DB span around handler execution.
    /// Returns a <see cref="SyncTestQuery.Result"/> carrying the query's id so tests can
    /// confirm the pipeline executed correctly.
    /// </summary>
    internal sealed class SyncHandlerWithDbSpanAttribute : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [QueryDbSpan(step: 1, DbSystem.PostgreSql, "orders", "order", "select")]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
            => new SyncTestQuery.Result { Value = query.Id };
    }
}
