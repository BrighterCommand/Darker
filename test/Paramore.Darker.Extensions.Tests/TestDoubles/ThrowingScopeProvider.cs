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

        public IAmAScope GetAmbient(ScopeAffinity affinity)
        {
            Interlocked.Increment(ref _callCount);
            throw new InvalidOperationException("The scope provider must not be asked when the affinity is AlwaysNew.");
        }
    }
}
