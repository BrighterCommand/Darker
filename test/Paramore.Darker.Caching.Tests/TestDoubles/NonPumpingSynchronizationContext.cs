#region Licence
/* The MIT License (MIT)
Copyright © 2026 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */
#endregion

using System.Collections.Concurrent;
using System.Threading;

namespace Paramore.Darker.Caching.Tests.TestDoubles;

/// <summary>
/// A <see cref="SynchronizationContext"/> test double that models a single-threaded context
/// (classic ASP.NET request thread, WPF/WinForms UI thread) which is <em>never pumped</em>.
/// <see cref="Post"/> merely queues the continuation; nothing ever runs it. So any async
/// continuation captured by this context — as happens when library code awaits without
/// <c>ConfigureAwait(false)</c> — will never execute. If code under test then blocks the
/// originating thread waiting for that continuation, it deadlocks.
/// </summary>
/// <remarks>
/// Unlike the base <see cref="SynchronizationContext"/> (whose <c>Post</c> dispatches to the
/// thread pool), this double deliberately does not dispatch, so it can reproduce the classic
/// sync-over-async deadlock deterministically.
/// </remarks>
public sealed class NonPumpingSynchronizationContext : SynchronizationContext
{
    private readonly ConcurrentQueue<(SendOrPostCallback callback, object state)> _queued = new();

    /// <summary>Gets the number of continuations captured by this context but never run.</summary>
    public int QueuedCount => _queued.Count;

    /// <inheritdoc />
    public override void Post(SendOrPostCallback d, object state) => _queued.Enqueue((d, state));

    /// <inheritdoc />
    public override void Send(SendOrPostCallback d, object state) => d(state);
}
