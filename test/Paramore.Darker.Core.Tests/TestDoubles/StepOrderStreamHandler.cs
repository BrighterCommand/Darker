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
    internal class StepOrderStreamQuery : IStreamQuery<string> { }

    /// <summary>
    /// A stream handler decorated with two <see cref="StreamStepEventAttribute"/>s to verify
    /// that <c>PipelineBuilder.BuildStream</c> orders decorators by step descending (step 2 → step 1 → handler).
    /// </summary>
    internal class StepOrderStreamHandler : IStreamQueryHandler<StepOrderStreamQuery, string>
    {
        public static readonly string[] Items = { "a", "b" };

        public IQueryContext Context { get; set; }

        [StreamStepEvent(step: 2)]
        [StreamStepEvent(step: 1)]
        public async IAsyncEnumerable<string> ExecuteAsync(
            StepOrderStreamQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
            await Task.CompletedTask;
        }
    }
}
