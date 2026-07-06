using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    /// <summary>
    /// Serialises every test that registers an <see cref="System.Diagnostics.ActivityListener"/> on
    /// the process-global <c>paramore.darker</c> <see cref="System.Diagnostics.ActivitySource"/>, so a
    /// leaked listener from one test cannot make the no-listener zero-overhead test observe a span
    /// (test isolation). Being DisableParallelization it also never runs concurrently with other
    /// activity-source tests in this assembly.
    /// </summary>
    [CollectionDefinition("DarkerActivitySource", DisableParallelization = true)]
    public sealed class DarkerActivitySourceCollection
    {
    }
}
