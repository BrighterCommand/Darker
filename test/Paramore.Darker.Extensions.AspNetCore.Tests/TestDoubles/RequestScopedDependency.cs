using System;

namespace Paramore.Darker.Extensions.AspNetCore.Tests.TestDoubles
{
    /// <summary>
    /// A Scoped dependency that records whether it has been disposed, so a test can tell who owns
    /// the request scope it was resolved from.
    /// </summary>
    public sealed class RequestScopedDependency : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }
}
