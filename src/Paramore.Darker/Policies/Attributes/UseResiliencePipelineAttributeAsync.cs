#region Licence
/* The MIT License (MIT)
Copyright © 2024 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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
    /// Applies a Polly V8 <see cref="Polly.ResiliencePipeline"/> to an asynchronous query handler.
    /// The named pipeline is resolved at execution time from the
    /// <see cref="IQueryContext.ResiliencePipeline"/> provider.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class UseResiliencePipelineAttributeAsync : QueryHandlerAttributeAsync
    {
        private readonly string _policy;
        private readonly bool _useTypePipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="UseResiliencePipelineAttributeAsync"/> class.
        /// </summary>
        /// <param name="step">The ordinal position of this decorator in the pipeline; higher executes first.</param>
        /// <param name="policy">The registry key of the resilience pipeline to apply.</param>
        /// <param name="useTypePipeline">
        /// When <c>true</c>, resolves a result-type-scoped pipeline (one instance per <c>(key, TResult)</c>);
        /// when <c>false</c> (the default), resolves the single shared pipeline registered under the key.
        /// </param>
        public UseResiliencePipelineAttributeAsync(int step, string policy, bool useTypePipeline = false)
            : base(step)
        {
            _policy = policy;
            _useTypePipeline = useTypePipeline;
        }

        /// <summary>
        /// Returns the parameters passed to the decorator: the pipeline key and the type-scope flag.
        /// </summary>
        /// <returns>An array containing the policy key and the <c>useTypePipeline</c> flag.</returns>
        public override object[] GetAttributeParams()
        {
            return new object[] { _policy, _useTypePipeline };
        }

        /// <summary>
        /// Returns the open generic type of the asynchronous decorator that applies the pipeline.
        /// </summary>
        /// <returns>The <see cref="UseResiliencePipelineHandlerAsync{TQuery,TResult}"/> open generic type.</returns>
        public override Type GetDecoratorType()
        {
            return typeof(UseResiliencePipelineHandlerAsync<,>);
        }
    }
}
