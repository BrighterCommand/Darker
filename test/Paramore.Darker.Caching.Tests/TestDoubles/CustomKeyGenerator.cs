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
/// A test-double <see cref="ICacheKeyGenerator"/> that records how many times
/// <see cref="GenerateKey"/> was called and returns a distinctive key shape so tests can
/// prove the custom generator was used by the pipeline end-to-end.
/// </summary>
public sealed class CustomKeyGenerator : ICacheKeyGenerator
{
    private int _callCount;

    /// <summary>Gets the number of times <see cref="GenerateKey"/> has been called.</summary>
    public int GenerateKeyCallCount => _callCount;

    /// <inheritdoc />
    public string GenerateKey(object query)
    {
        _callCount++;
        return $"custom|{query.GetType().Name}";
    }
}
