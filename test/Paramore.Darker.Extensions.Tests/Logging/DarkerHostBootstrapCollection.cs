using Xunit;

namespace Paramore.Darker.Extensions.Tests.Logging
{
    /// <summary>
    /// Serialises tests that bootstrap a Darker host via <c>AddDarker</c>, because that call resets the
    /// process-global <c>ApplicationLogging.LoggerFactory</c>; running them in parallel races on it
    /// (the consumer-side mitigation documented in C6 / the release notes).
    /// </summary>
    [CollectionDefinition("DarkerHostBootstrap", DisableParallelization = true)]
    public sealed class DarkerHostBootstrapCollection
    {
    }
}
