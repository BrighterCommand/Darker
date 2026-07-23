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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;

namespace Paramore.Darker.Caching.Tests.TestDoubles;

/// <summary>
/// A <see cref="HybridCache"/> test double whose
/// <see cref="GetOrCreateAsync{TState,T}"/> always suspends via
/// <c>await Task.Yield()</c> before invoking the factory. Because the method
/// yields before returning, the <see cref="System.Threading.Tasks.ValueTask{T}"/> it returns
/// is <em>not</em> <c>IsCompletedSuccessfully</c> at the call site. This forces the
/// <c>else</c> blocking-fallback branch in
/// <c>CacheableQueryDecorator.Execute</c> that calls
/// <c>.AsTask().GetAwaiter().GetResult()</c>.
/// </summary>
/// <remarks>
/// No caching is performed — the factory is always invoked, so the handler runs on
/// every <c>Execute</c> call. The other abstract members are no-ops that return
/// a completed <see cref="ValueTask"/> because they are not exercised by the
/// sync-fallback test.
/// </remarks>
public sealed class AsyncOnMissHybridCache : HybridCache
{
    /// <inheritdoc />
    /// <remarks>
    /// Yields before calling the factory so the returned <see cref="System.Threading.Tasks.ValueTask{T}"/>
    /// is never synchronously completed, exercising the blocking fallback in
    /// <c>CacheableQueryDecorator.Execute</c>.
    /// </remarks>
    public override async ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        // Force an async suspension so the ValueTask returned to the caller is NOT
        // IsCompletedSuccessfully — this is the condition that triggers the else branch
        // in CacheableQueryDecorator.Execute.
        await Task.Yield();
        return await factory(state, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    /// <inheritdoc />
    public override ValueTask RemoveAsync(
        string key,
        CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    /// <inheritdoc />
    public override ValueTask RemoveByTagAsync(
        string tag,
        CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
