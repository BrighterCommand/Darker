using Xunit;

namespace Paramore.Darker.Extensions.Diagnostics.Tests;

/// <summary>
/// Serialises every test that creates a process-global <see cref="System.Diagnostics.Metrics.MeterProvider"/>
/// on the <c>paramore.darker</c> meter, so a leaked provider from one test cannot interfere with
/// another (process-global meter / MeterProvider state isolation).
/// </summary>
[CollectionDefinition("DarkerMeter", DisableParallelization = true)]
public sealed class DarkerMeterCollection
{
}
