namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    // Closed over a tag type, the query, result and item types are distinct per fact, so the static
    // per-TResult logger in the pipeline is created against the logger factory current when that fact first runs.
    public sealed class LifetimeFaultTag { }
}
