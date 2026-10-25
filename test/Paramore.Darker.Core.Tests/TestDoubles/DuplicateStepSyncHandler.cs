namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A sync handler whose Execute carries two decorator attributes at the same Step.</summary>
    internal class DuplicateStepSyncHandler : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [FirstPassThrough(2)]
        [SecondPassThrough(2)]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
        {
            return new SyncTestQuery.Result { Value = query.Id };
        }
    }
}
