using System;
using Paramore.Darker.Observability;
using Paramore.Darker.Observability.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A sync handler whose <c>[QueryDbSpan]</c>-wrapped work throws, standing in for a failed database call.</summary>
    internal sealed class ThrowingSyncHandlerWithDbSpanAttribute : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [QueryDbSpan(step: 1, DbSystem.PostgreSql, "orders", "order", "select")]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
            => throw new InvalidOperationException("orders database unavailable");
    }
}
