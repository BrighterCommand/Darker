namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A minimal sync handler decorated with <see cref="SyncStepEventAttribute"/> used to verify
    /// that <c>PipelineBuilder.Build</c> writes a step event for the sink handler in the sync
    /// pipeline. Returns a <see cref="SyncTestQuery.Result"/> carrying the query's id so tests can
    /// confirm the pipeline executed correctly.
    /// </summary>
    internal sealed class SyncStepEventHandler : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [SyncStepEvent(step: 1)]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
            => new SyncTestQuery.Result { Value = query.Id };
    }
}
