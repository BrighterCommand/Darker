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

namespace Paramore.Darker.Caching.Tests.TestDoubles;

/// <summary>
/// A minimal query used by negative-caching tests. The handler for this query always returns
/// <c>null</c>, allowing tests to verify FR11: the caching decorator stores and serves a
/// <c>null</c> result as a hit without re-running the handler.
/// </summary>
/// <remarks>
/// The result type is a class (reference type) so that <c>null</c> is a valid runtime value.
/// The query uses the default cache-key strategy (type name + JSON of properties) so that
/// two calls with the same <see cref="QueryKey"/> value resolve to the same cache entry.
/// </remarks>
public sealed class NullReturningCacheQuery : IQuery<NullReturningCacheQuery.Result>
{
    /// <summary>Gets or sets the value included in the default cache key for this query.</summary>
    public string QueryKey { get; set; } = "null-returning-query";

    /// <summary>
    /// A reference-type result whose <c>null</c> value is what the handler stores in the cache.
    /// </summary>
    public sealed class Result
    {
        // Intentionally empty — only the null value matters for FR11 negative-caching tests.
    }
}
