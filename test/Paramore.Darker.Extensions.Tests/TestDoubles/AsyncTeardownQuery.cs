namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    // Each path has result types of its own, so the static per-TResult logger in the pipeline
    // is created against the logger factory of the test that first runs it.

    public sealed class AsyncTeardownQuery : IQuery<AsyncTeardownResult>
    {
    }
}
