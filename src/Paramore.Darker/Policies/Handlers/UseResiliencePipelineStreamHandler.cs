#region Licence
/* The MIT License (MIT)
Copyright © 2025 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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
using System.Collections.Generic;
using System.Threading;

namespace Paramore.Darker.Policies.Handlers
{
    /// <summary>
    /// Stream decorator that executes the establishment of a stream query through a named Polly V8
    /// <see cref="Polly.ResiliencePipeline"/> resolved from the query context provider.
    /// Resilience covers only stream establishment (the first <c>MoveNextAsync</c>); faults after
    /// the first item has been yielded propagate directly to the caller without retry.
    /// </summary>
    /// <typeparam name="TQuery">The stream query type.</typeparam>
    /// <typeparam name="TResult">The item type produced by the stream.</typeparam>
    public class UseResiliencePipelineStreamHandler<TQuery, TResult> : IStreamQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IStreamQuery<TResult>
    {
        /// <summary>
        /// The ambient query context, supplying the resilience pipeline provider and context.
        /// </summary>
        public IQueryContext Context { get; set; }

        /// <summary>
        /// Initializes the decorator from the attribute parameters (pipeline key).
        /// </summary>
        /// <param name="attributeParams">The parameters supplied by the attribute.</param>
        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            throw new NotImplementedException("UseResiliencePipelineStreamHandler is not yet implemented. It will be completed in a subsequent task.");
        }

        /// <summary>
        /// Executes the stream query through the resolved resilience pipeline.
        /// </summary>
        /// <param name="query">The stream query to execute.</param>
        /// <param name="next">The next step in the stream pipeline.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An async enumerable of result items.</returns>
        public IAsyncEnumerable<TResult> Execute(
            TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("UseResiliencePipelineStreamHandler is not yet implemented. It will be completed in a subsequent task.");
        }
    }
}
