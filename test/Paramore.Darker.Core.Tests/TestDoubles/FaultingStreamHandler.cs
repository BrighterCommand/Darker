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

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class FaultingStreamQuery : IStreamQuery<string> { }

    /// <summary>
    /// Yields a fixed number of items then throws, allowing tests to verify that exceptions
    /// mid-stream surface unwrapped (not as TargetInvocationException).
    /// </summary>
    internal class FaultingStreamHandler : IStreamQueryHandler<FaultingStreamQuery, string>
    {
        public const string ExceptionMessage = "Fault during stream enumeration";
        public const int ItemsBeforeFault = 2;

        public IQueryContext Context { get; set; }

#pragma warning disable CS1998
        public async IAsyncEnumerable<string> ExecuteAsync(
            FaultingStreamQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore CS1998
        {
            yield return "item-1";
            yield return "item-2";
            throw new InvalidOperationException(ExceptionMessage);
        }
    }
}
