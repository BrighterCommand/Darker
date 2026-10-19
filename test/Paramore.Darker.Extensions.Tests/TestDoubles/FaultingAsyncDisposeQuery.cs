namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    // Each path has result types of its own, so the static per-TResult logger in the pipeline is not shared.

    public sealed class FaultingAsyncDisposeQuery : IQuery<FaultingAsyncDisposeResult>
    {
    }
}
