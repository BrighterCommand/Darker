using Xunit;

namespace Paramore.Darker.Core.Tests
{
    /// <summary>
    /// Serialises every test that registers an <see cref="System.Diagnostics.ActivityListener"/> on
    /// the process-global <c>paramore.darker</c> <see cref="System.Diagnostics.ActivitySource"/>, so a
    /// leaked listener from one test cannot make the no-listener zero-overhead test observe a span
    /// (C5 test isolation). Being DisableParallelization it also never runs concurrently with the
    /// QueryLoggingJsonOptions collections.
    /// </summary>
    [CollectionDefinition("DarkerActivitySource", DisableParallelization = true)]
    public sealed class DarkerActivitySourceCollection
    {
    }
}
