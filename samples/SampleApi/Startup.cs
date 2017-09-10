using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.AspNetCore;
using Paramore.Darker.Policies;
using Paramore.Darker.QueryLogging;
using Polly;
using SampleApi.Domain;
using SampleApi.Ports;

namespace SampleApi
{
    public class Startup
    {
        internal const string SomethingWentTerriblyWrongCircuitBreakerName = "SomethingWentTerriblyWrongCircuitBreaker";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add Darker and some extensions.
            services.AddDarker()
                .AddHandlersFromAssemblies(typeof(GetPeopleQuery).Assembly)
                .AddJsonQueryLogging()
                .AddPolicies(ConfigurePolicies());

            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }

        private IPolicyRegistry ConfigurePolicies()
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

            return new PolicyRegistry
            {
                { Paramore.Darker.Policies.Constants.RetryPolicyName, defaultRetryPolicy },
                { Paramore.Darker.Policies.Constants.CircuitBreakerPolicyName, circuitBreakerPolicy },
                { SomethingWentTerriblyWrongCircuitBreakerName, circuitBreakTheWorstCaseScenario }
            };
        }
    }
}
