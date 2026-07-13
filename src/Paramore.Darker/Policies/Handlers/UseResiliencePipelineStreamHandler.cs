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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Exceptions;
using Polly;

namespace Paramore.Darker.Policies.Handlers
{
    /// <summary>
    /// Stream decorator that executes the establishment of a stream query through a named Polly V8
    /// <see cref="ResiliencePipeline"/> resolved from the query context provider.
    /// </summary>
    /// <remarks>
    /// Resilience covers only stream <em>establishment</em>: the call to <c>next</c> and the first
    /// <c>MoveNextAsync</c> run inside the pipeline boundary. Once the first item has been yielded the
    /// pipeline has already exited; subsequent <c>MoveNextAsync</c> faults propagate directly to the
    /// caller without retry. This gives a well-defined "no duplicate emission" guarantee — a retry
    /// before the first item always starts a fresh <see cref="IAsyncEnumerator{T}"/>.
    /// <para>
    /// Only the untyped <see cref="ResiliencePipeline"/> is supported (no <c>useTypePipeline</c>
    /// overload): a <c>ResiliencePipeline&lt;TResult&gt;</c> cannot wrap the
    /// <c>(IAsyncEnumerator&lt;TResult&gt;, bool)</c> tuple returned by the establishment callback.
    /// </para>
    /// </remarks>
    /// <typeparam name="TQuery">The stream query type.</typeparam>
    /// <typeparam name="TResult">The item type produced by the stream.</typeparam>
    public class UseResiliencePipelineStreamHandler<TQuery, TResult> : IStreamQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IStreamQuery<TResult>
    {
        private string _policy;

        /// <summary>
        /// The ambient query context, supplying the resilience pipeline provider and resilience context.
        /// </summary>
        public IQueryContext Context { get; set; }

        /// <summary>
        /// Initializes the decorator from the attribute parameters.
        /// </summary>
        /// <param name="attributeParams">
        /// A single-element array whose first element is the resilience pipeline key (a <see cref="string"/>).
        /// </param>
        /// <exception cref="ConfigurationException">
        /// Thrown when no resilience pipeline provider is configured on the query context, or when the
        /// named pipeline does not exist in the registry.
        /// </exception>
        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            _policy = (string)attributeParams[0];

            var provider = Context.ResiliencePipeline ?? throw new ConfigurationException(
                "No resilience pipeline provider is configured. Set a resilience pipeline registry on the query context or pass one to the QueryProcessor constructor.");

            if (!provider.TryGetPipeline(_policy, out _))
                throw new ConfigurationException($"Resilience pipeline does not exist in the registry: {_policy}");
        }

        /// <summary>
        /// Executes the stream query through the resolved resilience pipeline, yielding items only
        /// after establishment succeeds.
        /// </summary>
        /// <param name="query">The stream query to execute.</param>
        /// <param name="next">The next step in the stream pipeline.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An async enumerable of result items.</returns>
        public async IAsyncEnumerable<TResult> Execute(
            TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var pipeline = Context.ResiliencePipeline.GetPipeline(_policy);

            // Establish the stream + first pull inside the pipeline boundary.
            // On failure the enumerator is disposed before rethrowing so retried attempts don't leak.
            async ValueTask<(IAsyncEnumerator<TResult> enumerator, bool moved)> Establish(CancellationToken ct)
            {
                var e = next(query, ct).GetAsyncEnumerator(ct);
                try   { return (e, await e.MoveNextAsync().ConfigureAwait(false)); }
                catch { await e.DisposeAsync().ConfigureAwait(false); throw; }
            }

            // Mirror the context-vs-token branching from UseResiliencePipelineHandlerAsync.
            var resilienceContext = Context.ResilienceContext;
            var (enumerator, moved) = resilienceContext != null
                ? await pipeline.ExecuteAsync(ctx => Establish(ctx.CancellationToken), resilienceContext).ConfigureAwait(false)
                : await pipeline.ExecuteAsync(ct => Establish(ct), cancellationToken).ConfigureAwait(false);

            await using (enumerator.ConfigureAwait(false))
            {
                if (!moved) yield break;
                do { yield return enumerator.Current; }
                while (await enumerator.MoveNextAsync().ConfigureAwait(false));
            }
        }
    }
}
