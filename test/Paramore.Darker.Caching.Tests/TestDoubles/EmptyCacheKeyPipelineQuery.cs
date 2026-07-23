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
/// An end-to-end pipeline test double that implements both <see cref="IQuery{TResult}"/> and
/// <see cref="IAmCacheable"/>. Its <see cref="CacheKey"/> always returns an empty string at
/// runtime, which is the condition that triggers the fail-fast <c>ConfigurationException</c>
/// from <see cref="DefaultCacheKeyGenerator"/> (FR4).
/// </summary>
public sealed class EmptyCacheKeyPipelineQuery : IQuery<EmptyCacheKeyPipelineQuery.Result>, IAmCacheable
{
    /// <inheritdoc />
    /// <remarks>Deliberately returns an empty string to exercise the FR4 fail-fast path.</remarks>
    public string CacheKey => string.Empty;

    /// <summary>The result type returned by <see cref="EmptyCacheKeyPipelineQueryHandlerAsync"/>.</summary>
    public sealed class Result
    {
        /// <summary>Gets or sets the value produced by the handler.</summary>
        public string Value { get; set; } = string.Empty;
    }
}
