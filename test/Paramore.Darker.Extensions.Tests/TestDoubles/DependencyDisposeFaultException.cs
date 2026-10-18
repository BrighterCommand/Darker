using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A distinctive exception, so a test can pick the dependency's fault out of the captured logs.</summary>
    public sealed class DependencyDisposeFaultException : Exception
    {
        public DependencyDisposeFaultException() : base("dependency failed to dispose") { }
    }
}
