namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A second query/result type (returning <see cref="int"/>) used as the
    /// non-matching handler in the QueryProcessor "executes the matching handler"
    /// tests. Distinct in-test analogue of the scannable <c>Exported.TestQueryB</c>
    /// (copied and renamed per ADR 0013, Decision 1).
    /// </summary>
    internal class ProcessorIntQuery : IQuery<int>
    {
    }
}
