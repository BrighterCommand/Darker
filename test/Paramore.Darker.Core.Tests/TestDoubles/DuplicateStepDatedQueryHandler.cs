namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A sync handler for a second query whose Execute carries two decorator attributes at the same Step.</summary>
    internal class DuplicateStepDatedQueryHandler : QueryHandler<DatedQuery, string>
    {
        [FirstPassThrough(2)]
        [SecondPassThrough(2)]
        public override string Execute(DatedQuery query) => "duplicate";
    }
}
