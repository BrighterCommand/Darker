using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A minimal async handler decorated with <see cref="AsyncStepEventAttribute"/> used to verify
    /// that <c>PipelineBuilder.BuildAsync</c> writes a step event for the sink handler in the async
    /// pipeline. Returns an <see cref="AsyncTestQuery.Result"/> carrying the query's id so tests can
    /// confirm the pipeline executed correctly.
    /// </summary>
    internal sealed class AsyncStepEventHandler : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        [AsyncStepEvent(step: 1)]
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
    }
}
