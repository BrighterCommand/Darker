using System;
using System.Reflection;
using LightInject;
using LightInject.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Builder;
using Paramore.Darker.Decorators;
using Paramore.Darker.LightInject;
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
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var container = new ServiceContainer
            {
                ScopeManagerProvider = new StandaloneScopeManagerProvider()
            };

            // Configure and register Darker.
            var queryProcessor = QueryProcessorBuilder.With()
                .LightInjectHandlers(container, opts => opts
                    .WithQueriesAndHandlersFromAssembly(typeof(GetPeopleQueryHandler).GetTypeInfo().Assembly))
                .InMemoryQueryContextFactory()
                .JsonQueryLogging()
                .Policies(ConfigurePolicies())
                .Build();

            container.RegisterInstance(queryProcessor);

            // Don't forget to register the required decorators. todo maybe find a way to auto-discover these
            container.Register(typeof(QueryLoggingDecorator<,>));
            container.Register(typeof(RetryableQueryDecorator<,>));
            container.Register(typeof(FallbackPolicyDecorator<,>));

            // Add framework services.
            services.AddMvc();

            return container.CreateServiceProvider(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //loggerFactory.AddSerilog();

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
                { SomethingWentTerriblyWrongCircuitBreakerName, circuitBreakTheWorstCaseScenario },
            };
        }
    }
}
