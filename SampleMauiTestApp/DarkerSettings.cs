using Paramore.Darker.Policies;
using Polly;
using Polly.Registry;
using SampleMauiTestApp.Domain;

namespace SampleMauiTestApp;

public class DarkerSettings
{
    public const string SomethingWentTerriblyWrongCircuitBreakerName = "SomethingWentTerriblyWrongCircuitBreaker";

    public static IPolicyRegistry<string> ConfigurePolicies()
    {
        var defaultRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromMilliseconds(50),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(150)
            });

        var circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(1, TimeSpan.FromMilliseconds(500));

        var circuitBreakTheWorstCaseScenario = Policy
            .Handle<SomethingWentTerriblyWrongException>()
            .CircuitBreakerAsync(1, TimeSpan.FromSeconds(5));

        var policyRegistry = new PolicyRegistry
        {
            {Constants.RetryPolicyName, defaultRetryPolicy},
            {Constants.CircuitBreakerPolicyName, circuitBreakerPolicy},
            {SomethingWentTerriblyWrongCircuitBreakerName, circuitBreakTheWorstCaseScenario}
        };
        return policyRegistry;
    }
}