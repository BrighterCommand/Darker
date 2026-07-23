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

namespace Paramore.Darker.Caching;

/// <summary>
/// Indicates whether a cache lookup resulted in a hit or a miss.
/// </summary>
/// <remarks>
/// <para>
/// Used internally by <see cref="CacheableQueryDecoratorAsync{TQuery,TResult}"/> (and its
/// synchronous counterpart) to name the factory-ran determination from
/// <c>HybridCache.GetOrCreateAsync</c> before mapping it to the span-attribute string values
/// <c>"hit"</c> / <c>"miss"</c>.
/// </para>
/// <para>
/// <strong>Naming note:</strong> <c>CacheOutcome</c> is also the name of the string constant
/// <see cref="Paramore.Darker.Observability.DarkerSemanticConventions.CacheOutcome"/>, which
/// holds the span-attribute <em>key</em> (<c>"paramore.darker.cache.outcome"</c>). These are
/// distinct identifiers: this type is an <c>enum</c> in namespace
/// <c>Paramore.Darker.Caching</c>; the constant is a <c>string</c> in namespace
/// <c>Paramore.Darker.Observability</c>. When both are in scope, use fully-qualified names to
/// avoid ambiguity.
/// </para>
/// </remarks>
public enum CacheOutcome
{
    /// <summary>The cache returned a stored result; the handler and inner decorators did not run.</summary>
    Hit,

    /// <summary>No cached result existed; the handler was invoked and its result was stored.</summary>
    Miss,
}
