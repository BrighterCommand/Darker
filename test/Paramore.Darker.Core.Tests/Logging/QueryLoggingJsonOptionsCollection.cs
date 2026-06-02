using Xunit;

namespace Paramore.Darker.Core.Tests.Logging
{
    /// <summary>
    /// Serialises every test that mutates the process-global
    /// <c>QueryLoggingJsonOptions.Options</c> so they do not interfere (C5 test isolation).
    /// </summary>
    [CollectionDefinition("QueryLoggingJsonOptions", DisableParallelization = true)]
    public sealed class QueryLoggingJsonOptionsCollection
    {
    }
}
