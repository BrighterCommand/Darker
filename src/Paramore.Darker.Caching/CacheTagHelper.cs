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

using System.Collections.Generic;

namespace Paramore.Darker.Caching;

/// <summary>
/// Shared helper that reads the optional cache tag from <see cref="IQueryContext.Bag"/>.
/// Used by both the sync and async caching decorators so there is a single source of truth
/// for the tag-extraction logic.
/// </summary>
internal static class CacheTagHelper
{
    /// <summary>
    /// Reads the well-known <see cref="CacheableQueryAttribute.CacheTag"/> key from
    /// <paramref name="context"/>. When the value is a non-empty <see cref="string"/>, wraps it
    /// as a one-element array so it can be passed to
    /// <c>HybridCache.GetOrCreateAsync</c> as <c>IEnumerable&lt;string&gt; tags</c>.
    /// Returns <see langword="null"/> when the key is absent or the value is not a non-empty string
    /// (best-effort, per FR9 — no exception is thrown).
    /// </summary>
    /// <param name="context">The current query context.</param>
    /// <returns>A one-element array containing the tag, or <see langword="null"/>.</returns>
    internal static IEnumerable<string>? ReadTags(IQueryContext context)
    {
        if (context.Bag.TryGetValue(CacheableQueryAttribute.CacheTag, out var tagValue)
            && tagValue is string tag
            && !string.IsNullOrWhiteSpace(tag))
            return new[] { tag };

        return null;
    }
}
