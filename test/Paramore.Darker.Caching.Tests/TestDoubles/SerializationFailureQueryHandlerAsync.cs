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

using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Caching.Tests.TestDoubles;

/// <summary>
/// A test-double async handler for <see cref="SerializationFailureQuery"/> whose
/// <c>ExecuteAsync</c> is annotated with <see cref="CacheableQueryAttributeAsync"/> so that
/// the caching decorator is wired into the Darker pipeline automatically.
/// On a cache miss the handler returns a <see cref="SerializationFailureResult"/>; HybridCache
/// then attempts to serialize it using the registered <see cref="ThrowingHybridCacheSerializer"/>,
/// which throws. That exception must surface to the caller — not be swallowed by the decorator.
/// </summary>
public sealed class SerializationFailureQueryHandlerAsync
    : QueryHandlerAsync<SerializationFailureQuery, SerializationFailureResult>
{
    /// <inheritdoc />
    [CacheableQueryAttributeAsync(1, 60)]
    public override Task<SerializationFailureResult> ExecuteAsync(
        SerializationFailureQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SerializationFailureResult { Value = query.Payload });
    }
}
