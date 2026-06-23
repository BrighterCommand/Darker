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
using Paramore.Darker.Exceptions;

namespace Paramore.Darker.Policies.Handlers
{
    /// <summary>
    /// Synchronous decorator that executes a query handler through a named Polly V8
    /// <see cref="Polly.ResiliencePipeline"/> resolved from the query context provider.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResult">The result type returned by the query.</typeparam>
    public class UseResiliencePipelineHandler<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private string _policy;
        private bool _useTypePipeline;

        /// <summary>
        /// The ambient query context, supplying the resilience pipeline provider and context.
        /// </summary>
        public IQueryContext Context { get; set; }

        /// <summary>
        /// Initializes the decorator from the attribute parameters (pipeline key and type-scope flag).
        /// </summary>
        /// <param name="attributeParams">The parameters supplied by the attribute.</param>
        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            _policy = (string)attributeParams[0];
            _useTypePipeline = (bool)attributeParams[1];

            var provider = Context.ResiliencePipeline ?? throw new ConfigurationException(
                "No resilience pipeline provider is configured. Set a resilience pipeline registry on the query context or pass one to the QueryProcessor constructor.");

            var resolved = _useTypePipeline
                ? provider.TryGetPipeline<TResult>(_policy, out _)
                : provider.TryGetPipeline(_policy, out _);

            if (!resolved)
                throw new ConfigurationException($"Resilience pipeline does not exist in the registry: {_policy}");
        }

        /// <summary>
        /// Executes the query through the resolved resilience pipeline.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="next">The next step in the pipeline.</param>
        /// <param name="fallback">The fallback step in the pipeline.</param>
        /// <returns>The result of executing the query through the pipeline.</returns>
        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            return _useTypePipeline
                ? Context.ResiliencePipeline.GetPipeline<TResult>(_policy).Execute(() => next(query))
                : Context.ResiliencePipeline.GetPipeline(_policy).Execute(() => next(query));
        }
    }
}
