using System;
using System.Threading.Tasks;
using Polly;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A minimal Polly V8 resilience strategy that records a property read from the
    /// <see cref="ResilienceContext"/> it executes under. Used to prove that a caller-supplied
    /// resilience context (and its properties) flows through the pipeline.
    /// </summary>
    internal sealed class PropertyCapturingStrategy : ResilienceStrategy
    {
        private readonly ResiliencePropertyKey<string> _key;

        public PropertyCapturingStrategy(ResiliencePropertyKey<string> key)
        {
            _key = key;
        }

        public string CapturedValue { get; private set; }

        protected override ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
            Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
            ResilienceContext context,
            TState state)
        {
            if (context.Properties.TryGetValue(_key, out var value))
                CapturedValue = value;

            return callback(context, state);
        }
    }
}
