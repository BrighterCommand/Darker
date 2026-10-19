using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A distinctive exception, so a test can pick the dependency's async fault out of the captured logs.</summary>
    public sealed class DependencyDisposeAsyncFaultException : Exception
    {
        public DependencyDisposeAsyncFaultException() : base("dependency failed to dispose asynchronously") { }
    }
}
