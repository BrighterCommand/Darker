using Xunit;

namespace Paramore.Darker.Core.Tests.Logging
{
    /// <summary>
    /// Isolates the FR14 lock-after-use ordering test (AC3). That test deliberately triggers the
    /// irreversible <c>JsonSerializerOptions</c> lock on the process-global
    /// <c>QueryLoggingJsonOptions.Options</c>, so it runs alone in its own non-parallel collection —
    /// distinct from the <c>QueryLoggingJsonOptions</c> collection used by the save-and-restore tests —
    /// and must be sequenced last among the option-mutating tests.
    /// </summary>
    [CollectionDefinition("QueryLoggingJsonOptionsOrdering", DisableParallelization = true)]
    public sealed class QueryLoggingJsonOptionsOrderingCollection
    {
    }
}
