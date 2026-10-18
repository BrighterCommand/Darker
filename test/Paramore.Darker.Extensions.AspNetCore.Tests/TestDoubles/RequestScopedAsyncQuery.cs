namespace Paramore.Darker.Extensions.AspNetCore.Tests.TestDoubles
{
    /// <summary>An asynchronous query whose handler returns the dependency it was constructed with.</summary>
    public sealed class RequestScopedAsyncQuery : IQuery<RequestScopedDependency>
    {
    }
}
