// Copyright (c) 2025, Ian Cooper
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the
// following conditions are met:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// Wraps an <see cref="IAsyncEnumerable{T}"/> and invokes a callback each time
    /// <see cref="IAsyncEnumerator{T}.DisposeAsync"/> is called on a created enumerator.
    /// Used to assert that every enumerator created during resilience retries is disposed.
    /// </summary>
    internal sealed class DisposalCountingEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _inner;
        private readonly Action _onDispose;

        public DisposalCountingEnumerable(IAsyncEnumerable<T> inner, Action onDispose)
        {
            _inner = inner;
            _onDispose = onDispose;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new CountingEnumerator(_inner.GetAsyncEnumerator(cancellationToken), _onDispose);

        private sealed class CountingEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _inner;
            private readonly Action _onDispose;

            public CountingEnumerator(IAsyncEnumerator<T> inner, Action onDispose)
            {
                _inner = inner;
                _onDispose = onDispose;
            }

            public T Current => _inner.Current;

            public ValueTask<bool> MoveNextAsync() => _inner.MoveNextAsync();

            public async ValueTask DisposeAsync()
            {
                await _inner.DisposeAsync().ConfigureAwait(false);
                _onDispose();
            }
        }
    }
}
