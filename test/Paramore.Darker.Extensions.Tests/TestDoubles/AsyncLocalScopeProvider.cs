#nullable enable annotations
using System;
using System.Threading;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A scope provider that offers whatever scope the current async flow has made ambient, the way a
    /// non-ASP.NET host (a worker, a test) might. <see cref="Use"/> makes a scope ambient until the
    /// returned handle is disposed.
    /// </summary>
    public sealed class AsyncLocalScopeProvider : IAmAScopeProvider
    {
        private static readonly AsyncLocal<IAmAScope?> s_ambient = new();

        public IAmAScope? GetAmbient(ScopeAffinity affinity) => s_ambient.Value;

        public static IDisposable Use(IAmAScope scope)
        {
            var previous = s_ambient.Value;
            s_ambient.Value = scope;
            return new Restore(previous);
        }

        private sealed class Restore(IAmAScope? previous) : IDisposable
        {
            public void Dispose() => s_ambient.Value = previous;
        }
    }
}
