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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A counting stream test double that throws <see cref="InvalidOperationException"/> on its
    /// first <c>failuresBeforeSuccess</c> invocations (before yielding any item) and yields all
    /// <see cref="MultiItemStreamHandler.Items"/> on subsequent invocations. Used to prove a retry
    /// resilience pipeline retries a transient stream establishment failure to success with no
    /// duplicate item emission.
    /// </summary>
    internal sealed class TransientlyFailingStreamHandler : IStreamQueryHandler<MultiItemStreamQuery, string>
    {
        private readonly int _failuresBeforeSuccess;

        public TransientlyFailingStreamHandler(int failuresBeforeSuccess = 1)
        {
            _failuresBeforeSuccess = failuresBeforeSuccess;
        }

        /// <summary>Gets the total number of times <c>ExecuteAsync</c> has been entered.</summary>
        public int Calls { get; private set; }

        public IQueryContext Context { get; set; }

        public async IAsyncEnumerable<string> ExecuteAsync(
            MultiItemStreamQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Calls <= _failuresBeforeSuccess)
                throw new InvalidOperationException("transient stream failure");

            foreach (var item in MultiItemStreamHandler.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
            await Task.CompletedTask;
        }
    }
}
