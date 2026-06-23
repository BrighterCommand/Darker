namespace Paramore.Darker.Policies
{
    public static class Constants
    {
        public const string RetryPolicyName = "Darker.RetryPolicy";
        public const string CircuitBreakerPolicyName = "Darker.CircuitBreakerPolicy";
        /// <summary>The well-known registry key for the default retry resilience pipeline.</summary>
        public const string RetryPipelineName = "Darker.RetryPipeline";

        /// <summary>The well-known registry key for the default circuit-breaker resilience pipeline.</summary>
        public const string CircuitBreakerPipelineName = "Darker.CircuitBreakerPipeline";
    }
}