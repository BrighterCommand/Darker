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
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A custom Polly v8 resilience strategy that acts as a fallback for stream establishment.
    /// When the wrapped callback faults, it invokes <see cref="_fallbackFactory"/> and returns
    /// its result as a successful outcome. Used to test that
    /// <c>UseResiliencePipelineStreamHandler</c> correctly hands the fallback-supplied
    /// <c>(IAsyncEnumerator&lt;T&gt;, bool)</c> to the outer <c>await using</c> without leaking
    /// the (already-disposed) primary enumerator.
    /// </summary>
    /// <remarks>
    /// The untyped <see cref="ResiliencePipeline"/> boxes execution results to <c>object</c>
    /// internally, so <typeparamref name="TResult"/> inside <see cref="ExecuteCore{TResult,TState}"/>
    /// is always <c>object</c> when used with the untyped pipeline. The factory therefore returns
    /// a boxed tuple that Polly unboxes back to <c>(IAsyncEnumerator&lt;T&gt;, bool)</c> for the
    /// caller.
    /// </remarks>
    internal sealed class EstablishmentFallbackStrategy : ResilienceStrategy
    {
        private readonly Func<CancellationToken, ValueTask<object>> _fallbackFactory;

        public EstablishmentFallbackStrategy(Func<CancellationToken, ValueTask<object>> fallbackFactory)
        {
            _fallbackFactory = fallbackFactory;
        }

        protected override async ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
            Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
            ResilienceContext context,
            TState state)
        {
            var outcome = await callback(context, state);
            if (outcome.Exception != null)
            {
                var fallbackResult = await _fallbackFactory(context.CancellationToken);
                return Outcome.FromResult((TResult)fallbackResult);
            }
            return outcome;
        }
    }
}
