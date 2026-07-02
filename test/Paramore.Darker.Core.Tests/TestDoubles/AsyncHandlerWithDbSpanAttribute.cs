using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Observability;
using Paramore.Darker.Observability.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A minimal async handler decorated with <see cref="QueryDbSpanAttributeAsync"/> used to verify
    /// that the pipeline weaves a <c>QueryDbSpanDecoratorAsync</c> child DB span around handler execution.
    /// Returns an <see cref="AsyncTestQuery.Result"/> carrying the query's id so tests can
    /// confirm the pipeline executed correctly.
    /// </summary>
    internal sealed class AsyncHandlerWithDbSpanAttribute : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        [QueryDbSpanAttributeAsync(step: 1, DbSystem.MsSql, "orders", "order", "select")]
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
    }
}
