using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A service whose <see cref="Dispose"/> throws, with its own query so the pipeline's closed generic is disjoint.</summary>
    public sealed class ThrowingDisposeDependency : IDisposable
    {
        public void Dispose() => throw new DependencyDisposeFaultException();
    }
}
