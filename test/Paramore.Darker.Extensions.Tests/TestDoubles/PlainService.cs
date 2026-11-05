namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public service that is neither a handler nor a decorator, and takes a <see cref="ScopedDependency"/>.</summary>
    public sealed class PlainService
    {
        public PlainService(ScopedDependency dependency)
        {
        }
    }
}
