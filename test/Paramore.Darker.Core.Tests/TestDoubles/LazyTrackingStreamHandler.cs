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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class LazyStreamQuery : IStreamQuery<int> { }

    /// <summary>
    /// Handler that increments a shared counter before each yield, allowing tests to
    /// observe how many items have been produced at any given point during enumeration.
    /// </summary>
    internal class LazyTrackingStreamHandler : IStreamQueryHandler<LazyStreamQuery, int>
    {
        public const int TotalItems = 5;

        private readonly int[] _producedCount;

        public LazyTrackingStreamHandler(int[] producedCount) => _producedCount = producedCount;

        public IQueryContext Context { get; set; }

        public async IAsyncEnumerable<int> ExecuteAsync(
            LazyStreamQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            for (int i = 1; i <= TotalItems; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _producedCount[0]++;
                yield return i;
            }
            await Task.CompletedTask;
        }
    }
}
