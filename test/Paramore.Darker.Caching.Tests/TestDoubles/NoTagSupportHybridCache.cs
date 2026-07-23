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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;

namespace Paramore.Darker.Caching.Tests.TestDoubles;

/// <summary>
/// A <see cref="HybridCache"/> test double that stores entries in memory so that a second
/// call with the same key is a cache hit, but whose tag support is absent:
/// <see cref="RemoveByTagAsync"/> is a no-op (does not evict any entries) and the
/// <c>tags</c> argument on <see cref="GetOrCreateAsync{TState,T}"/> is accepted without
/// being used for eviction.
/// </summary>
/// <remarks>
/// This models the FR14 best-effort tagging contract: tagging against an implementation
/// without tag support still caches the entry and never fails the query. The key property
/// under test is that <see cref="GetOrCreateAsync{TState,T}"/> stores and serves entries
/// correctly so the second call is a hit (handler count 1), while
/// <see cref="RemoveByTagAsync"/> silently does nothing and the entry remains cached.
/// </remarks>
public sealed class NoTagSupportHybridCache : HybridCache
{
    private readonly ConcurrentDictionary<string, object?> _store = new();

    /// <inheritdoc />
    /// <remarks>
    /// Tags are accepted but silently ignored — this implementation has no tag-eviction
    /// support. Entries are stored and served from an in-memory dictionary keyed by
    /// <paramref name="key"/> so that the second call with the same key is a cache hit.
    /// </remarks>
    public override async ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out var cached))
            return (T)cached!;

        var value = await factory(state, cancellationToken).ConfigureAwait(false);
        _store[key] = value;
        return value;
    }

    /// <inheritdoc />
    public override ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        _store[key] = value;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// No-op: this implementation does not support tag-based eviction. The cached entry
    /// is retained after calling this method — consistent with FR14's best-effort tagging
    /// stance that supplying a tag never fails the query even when the underlying cache
    /// ignores it.
    /// </remarks>
    public override ValueTask RemoveByTagAsync(
        string tag,
        CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
