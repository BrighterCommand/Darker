namespace Paramore.Darker.Extensions.AspNetCore.Tests.TestDoubles
{
    /// <summary>A stream query whose handler yields the dependency it was constructed with.</summary>
    public sealed class RequestScopedStreamQuery : IStreamQuery<RequestScopedDependency>
    {
    }
}
