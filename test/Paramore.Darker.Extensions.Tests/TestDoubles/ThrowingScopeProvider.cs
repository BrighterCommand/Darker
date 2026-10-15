using System;
using System.Threading;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A scope provider that fails whenever it is asked for the ambient scope, and counts how often it
    /// was asked. A query that never asks it cannot be failed by it.
    /// </summary>
    public sealed class ThrowingScopeProvider : IAmAScopeProvider
    {
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        /// <summary>The exception most recently thrown, so a test can check it is wrapped, not replaced.</summary>
        public Exception Thrown { get; private set; }

        public IAmAScope GetAmbient(ScopeAffinity affinity)
        {
            Interlocked.Increment(ref _callCount);
            Thrown = new InvalidOperationException("The scope provider failed.");
            throw Thrown;
        }
    }
}
