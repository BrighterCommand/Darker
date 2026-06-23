using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using Polly.Retry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class When_AddResiliencePipelines_called_should_register_provider_and_decorators
    {
        private static ResiliencePipelineRegistry<string> RetryRegistry()
        {
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder("Retry", (builder, _) =>
                builder.AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.Zero
                }));
            return registry;
        }

        [Fact]
        public void AddResiliencePipelines_with_null_registry_should_throw_ArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            var builder = services.AddDarker();

            // Act / Assert
            Should.Throw<ArgumentNullException>(() => builder.AddResiliencePipelines(null));
        }

        [Fact]
        public void AddResiliencePipelines_should_register_the_registry_as_the_provider_service()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            var registry = RetryRegistry();
            services.AddDarker()
                .AddHandlers(r => r.Register<SyncTestQuery, SyncTestQuery.Result, ResiliencePipelineRetryHandler>())
                .AddResiliencePipelines(registry);
            services.AddTransient<ResiliencePipelineRetryHandler>();

            var provider = services.BuildServiceProvider();

            // Act
            var resolved = provider.GetService<ResiliencePipelineProvider<string>>();

            // Assert — the supplied registry is registered as the provider service
            resolved.ShouldBeSameAs(registry);
        }

        [Fact]
        public void AddResiliencePipelines_should_flow_provider_into_query_execution()
        {
            // Arrange — a decorated handler that fails transiently, plus a retry pipeline
            var id = Guid.NewGuid();
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddHandlers(r => r.Register<SyncTestQuery, SyncTestQuery.Result, ResiliencePipelineRetryHandler>())
                .AddResiliencePipelines(RetryRegistry());
            services.AddTransient<ResiliencePipelineRetryHandler>();

            var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act — end-to-end: proves the provider reached the IQueryContext, not merely that a
            // service was registered (the handler is retried to success through the named pipeline)
            var result = queryProcessor.Execute(new SyncTestQuery(id));

            // Assert
            result.ShouldNotBeNull();
            result.Value.ShouldBe(id);
        }
    }
}
