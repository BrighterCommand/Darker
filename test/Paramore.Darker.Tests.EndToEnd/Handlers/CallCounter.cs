// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System.Threading;

namespace Paramore.Darker.Tests.EndToEnd.Handlers
{
    /// <summary>A thread-safe count of handler calls, registered as a Singleton.</summary>
    public sealed class CallCounter
    {
        private int _count;

        /// <summary>Gets the number of calls recorded so far.</summary>
        public int Count => Volatile.Read(ref _count);

        /// <summary>Records a call.</summary>
        /// <returns>The number of calls recorded, including this one.</returns>
        public int Increment() => Interlocked.Increment(ref _count);
    }
}
