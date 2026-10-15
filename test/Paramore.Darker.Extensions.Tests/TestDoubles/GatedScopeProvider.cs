#nullable enable annotations
using System;
using System.Threading;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A scope provider that holds every caller at a gate until the test releases it, so that many
    /// queries reach the probe at the same moment. The callers block, so a test runs its queries on
    /// dedicated threads rather than thread-pool threads.
    /// </summary>
    public sealed class GatedScopeProvider(int expectedCallers) : IAmAScopeProvider
    {
        private readonly CountdownEvent _arrived = new(expectedCallers);
        private readonly ManualResetEventSlim _gate = new(false);

        /// <summary>The scope every released caller is offered.</summary>
        public IAmAScope? Offered { get; set; }

        public bool WaitForAllToArrive(TimeSpan timeout) => _arrived.Wait(timeout);

        public void Release() => _gate.Set();

        public IAmAScope? GetAmbient(ScopeAffinity affinity)
        {
            _arrived.Signal();
            _gate.Wait();
            return Offered;
        }
    }
}
