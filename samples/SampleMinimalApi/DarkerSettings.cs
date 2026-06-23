using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Retry;
using SampleMinimalApi.Domain;

namespace SampleMinimalApi;

public class DarkerSettings
{
    /// <summary>
    /// General-purpose pipeline applied to the read handlers: retry transient faults, then break the
    /// circuit if they persist. Handlers refer to this key from their
    /// <c>[UseResiliencePipelineAttributeAsync]</c> decorator.
    /// </summary>
    public const string GeneralPipelineName = "Sample.GeneralPipeline";

    /// <summary>
    /// Retry-only pipeline used by the handler that simulates a transient failure: the first attempt
    /// throws and the retry recovers it.
    /// </summary>
    public const string RetryPipelineName = "Sample.RetryPipeline";

    /// <summary>
    /// Builds an explicit Polly v8 <see cref="ResiliencePipelineRegistry{TKey}"/>.
    /// <para>
    /// This is the V5 replacement for the legacy <c>IPolicyRegistry</c> approach. Instead of
    /// registering separate retry and circuit-breaker policies and relying on the default policy
    /// keys, we compose strategies into explicitly named resilience pipelines that the handlers opt
    /// into by key.
    /// </para>
    /// </summary>
    public static ResiliencePipelineRegistry<string> ConfigureResiliencePipelines()
    {
        var registry = new ResiliencePipelineRegistry<string>();

        registry.TryAddBuilder(GeneralPipelineName, (builder, _) =>
            builder
                // Strategies added first are outermost: retry wraps the circuit breaker.
                // Retry transient failures a few times with a short exponential backoff.
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(50),
                    BackoffType = DelayBackoffType.Exponential
                })
                // If failures keep happening, break the circuit so we stop hammering the source.
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = 2,
                    BreakDuration = TimeSpan.FromSeconds(5)
                }));

        registry.TryAddBuilder(RetryPipelineName, (builder, _) =>
            builder.AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<SomethingWentTerriblyWrongException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(50),
                BackoffType = DelayBackoffType.Exponential
            }));

        return registry;
    }
}
