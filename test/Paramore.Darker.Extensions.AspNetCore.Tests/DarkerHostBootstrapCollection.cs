using Xunit;

namespace Paramore.Darker.Extensions.AspNetCore.Tests
{
    /// <summary>
    /// Serialises tests that bootstrap a Darker host via <c>AddDarker</c>, because that call resets the
    /// process-global <c>ApplicationLogging.LoggerFactory</c>; running them in parallel races on it.
    /// </summary>
    [CollectionDefinition("DarkerHostBootstrap", DisableParallelization = true)]
    public sealed class DarkerHostBootstrapCollection
    {
    }
}
