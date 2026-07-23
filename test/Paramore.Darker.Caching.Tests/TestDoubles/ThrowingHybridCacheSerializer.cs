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
using System.Buffers;
using Microsoft.Extensions.Caching.Hybrid;

namespace Paramore.Darker.Caching.Tests.TestDoubles;

/// <summary>
/// A test-double <see cref="IHybridCacheSerializer{T}"/> for
/// <see cref="SerializationFailureResult"/> that always throws
/// <see cref="NotSupportedException"/> from both <see cref="Serialize"/> and
/// <see cref="Deserialize"/>.
/// </summary>
/// <remarks>
/// Register this serializer for <see cref="SerializationFailureResult"/> via
/// <c>AddHybridCache().AddSerializer&lt;SerializationFailureResult, ThrowingHybridCacheSerializer&gt;()</c>
/// to cause HybridCache to throw when it attempts to serialize the handler's result on a
/// cache miss. This verifies FR13: the caching decorator does not swallow serialization
/// exceptions — they surface to the caller unswallowed.
/// </remarks>
public sealed class ThrowingHybridCacheSerializer : IHybridCacheSerializer<SerializationFailureResult>
{
    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown to simulate a serialization failure.</exception>
    public void Serialize(SerializationFailureResult value, IBufferWriter<byte> target)
        => throw new NotSupportedException(
            "ThrowingHybridCacheSerializer: serialization is intentionally unsupported (FR13 test double).");

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown to simulate a deserialization failure.</exception>
    public SerializationFailureResult Deserialize(ReadOnlySequence<byte> source)
        => throw new NotSupportedException(
            "ThrowingHybridCacheSerializer: deserialization is intentionally unsupported (FR13 test double).");
}
