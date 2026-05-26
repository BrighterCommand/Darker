using System;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Policies;
using Polly;
using Polly.Registry;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    public static class PolicyDIExtensions
    {
        public static IDarkerHandlerBuilder AddPolicies(this IDarkerHandlerBuilder builder, IPolicyRegistry<string> policyRegistry)
        {
            QueryProcessorBuilderExtensions.AddPolicies(builder, policyRegistry);
            builder.Services.AddSingleton<IPolicyRegistry<string>>(policyRegistry);
            return builder;
        }

        public static IDarkerHandlerBuilder AddDefaultPolicies(this IDarkerHandlerBuilder builder)
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

            var policyRegistry = new PolicyRegistry
            {
                { Constants.RetryPolicyName, defaultRetryPolicy },
                { Constants.CircuitBreakerPolicyName, circuitBreakerPolicy }
            };

            return AddPolicies(builder, policyRegistry);
        }
    }
}
