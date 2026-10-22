namespace Paramore.Darker.Core.Tests.TestDoubles
{
    // Each path has a result type of its own, so the static per-TResult logger in the pipeline is not shared.

    internal sealed class ThrowingScopeQuery : IQuery<ThrowingScopeResult>
    {
    }
}
