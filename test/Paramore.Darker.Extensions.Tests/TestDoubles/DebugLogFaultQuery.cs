namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    // Closed over a tag, so each fact gets its own PipelineBuilder<TResult> and so its own static logger.
    public sealed class DebugLogFaultQuery<TTag> : IQuery<DebugLogFaultResult<TTag>>
    {
    }
}
