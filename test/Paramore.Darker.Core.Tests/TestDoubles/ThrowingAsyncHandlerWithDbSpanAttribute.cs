using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Observability;
using Paramore.Darker.Observability.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>An async handler whose <c>[QueryDbSpanAttributeAsync]</c>-wrapped work faults, standing in for a failed database call.</summary>
    internal sealed class ThrowingAsyncHandlerWithDbSpanAttribute : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        [QueryDbSpanAttributeAsync(step: 1, DbSystem.PostgreSql, "orders", "order", "select")]
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromException<AsyncTestQuery.Result>(new InvalidOperationException("orders database unavailable"));
    }
}
