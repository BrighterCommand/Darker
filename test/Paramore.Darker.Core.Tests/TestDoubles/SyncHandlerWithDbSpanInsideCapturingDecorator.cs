using Paramore.Darker.Observability;
using Paramore.Darker.Observability.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A sync handler whose <c>[QueryDbSpan]</c> step (step 2) runs inside a
    /// <see cref="CurrentActivityCapturingDecorator{TQuery,TResult}"/> (step 1, outermost).
    /// </summary>
    internal sealed class SyncHandlerWithDbSpanInsideCapturingDecorator : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [CurrentActivityCapturing(step: 1)]
        [QueryDbSpan(step: 2, DbSystem.PostgreSql, "orders", "order", "select")]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
            => new SyncTestQuery.Result { Value = query.Id };
    }
}
