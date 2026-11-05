namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public, decorator-free handler whose container descriptor a test removes.</summary>
    public sealed class RemovedHandlerQueryHandler : QueryHandler<RemovedHandlerQuery, RemovedHandlerResult>
    {
        public override RemovedHandlerResult Execute(RemovedHandlerQuery query) => new RemovedHandlerResult();
    }
}
