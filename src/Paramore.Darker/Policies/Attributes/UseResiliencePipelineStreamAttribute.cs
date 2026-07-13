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
using Paramore.Darker.Policies.Handlers;

namespace Paramore.Darker.Policies.Attributes
{
    /// <summary>
    /// Applies a Polly V8 <see cref="Polly.ResiliencePipeline"/> to a streaming query handler.
    /// The named pipeline covers only stream establishment (calling <c>next</c> and pulling the first
    /// item); faults after the first item has been yielded are not retried. Untyped pipelines only —
    /// there is no <c>useTypePipeline</c> overload for streams.
    /// </summary>
    /// <remarks>
    /// Use this attribute on a stream handler's <c>ExecuteAsync</c> method. The resilience pipeline
    /// is resolved at execution time from <see cref="IQueryContext.ResiliencePipeline"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class UseResiliencePipelineStreamAttribute : StreamQueryHandlerAttribute
    {
        private readonly string _policy;

        /// <summary>
        /// Initializes a new instance of the <see cref="UseResiliencePipelineStreamAttribute"/> class.
        /// </summary>
        /// <param name="step">The ordinal position of this decorator in the pipeline; higher executes first.</param>
        /// <param name="policy">The registry key of the untyped resilience pipeline to apply.</param>
        public UseResiliencePipelineStreamAttribute(int step, string policy)
            : base(step)
        {
            _policy = policy;
        }

        /// <summary>
        /// Returns the parameters passed to the decorator: the pipeline key.
        /// </summary>
        /// <returns>An array containing the policy key.</returns>
        public override object[] GetAttributeParams() => new object[] { _policy };

        /// <summary>
        /// Returns the open generic type of the stream decorator that applies the resilience pipeline.
        /// </summary>
        /// <returns>The <see cref="UseResiliencePipelineStreamHandler{TQuery,TResult}"/> open generic type.</returns>
        public override Type GetDecoratorType() => typeof(UseResiliencePipelineStreamHandler<,>);
    }
}
