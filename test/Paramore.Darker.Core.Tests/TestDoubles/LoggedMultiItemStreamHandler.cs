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
using Paramore.Darker.Logging.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A stream handler decorated with <see cref="StreamQueryLoggingAttribute"/> for use in
    /// logging lifecycle tests.
    /// </summary>
    internal sealed class LoggedMultiItemStreamHandler : IStreamQueryHandler<MultiItemStreamQuery, string>
    {
        public IQueryContext Context { get; set; }

        [StreamQueryLogging(1)]
        public async IAsyncEnumerable<string> ExecuteAsync(
            MultiItemStreamQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in MultiItemStreamHandler.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
            await Task.CompletedTask;
        }
    }
}
