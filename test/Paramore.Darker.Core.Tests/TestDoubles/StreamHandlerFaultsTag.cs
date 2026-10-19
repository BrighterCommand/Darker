namespace Paramore.Darker.Core.Tests.TestDoubles
{
    // Closed over a tag type, the query, result and item types are distinct per fact, so the static
    // per-TResult logger in the pipeline is not shared between facts.
    internal sealed class StreamHandlerFaultsTag { }
}
