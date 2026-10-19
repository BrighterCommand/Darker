namespace Paramore.Darker.Extensions.AspNetCore.Tests.TestDoubles
{
    /// <summary>A synchronous query whose handler returns the dependency it was constructed with.</summary>
    public sealed class RequestScopedSyncQuery : IQuery<RequestScopedDependency>
    {
    }
}
