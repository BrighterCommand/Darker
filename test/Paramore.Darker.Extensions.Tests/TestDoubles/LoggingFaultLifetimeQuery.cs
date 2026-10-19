namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    // Each query has a result type of its own, so the static per-TResult logger in the pipeline is
    // created against whichever logger factory is current when that query first runs.

    public sealed class LoggingFaultLifetimeQuery : IQuery<LoggingFaultLifetimeResult>
    {
    }
}
