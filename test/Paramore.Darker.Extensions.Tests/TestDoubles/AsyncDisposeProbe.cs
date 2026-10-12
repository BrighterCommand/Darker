namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A Singleton the test can read after the query completes, to see whether the per-query
    /// <see cref="YieldingAsyncOnlyDisposableDependency"/> had its DisposeAsync run to completion.
    /// </summary>
    public sealed class AsyncDisposeProbe
    {
        public volatile bool DisposedAsync;
    }
}
