using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Policies;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class When_AddDefaultResiliencePipelines_called_should_register_resolvable_default_pipelines
    {
        [Fact]
        public void AddDefaultResiliencePipelines_should_register_resolvable_executable_pipelines()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddHandlers(r => r.Register<SyncTestQuery, SyncTestQuery.Result, ResiliencePipelineRetryHandler>())
                .AddDefaultResiliencePipelines();
            services.AddTransient<ResiliencePipelineRetryHandler>();

            var provider = services.BuildServiceProvider();

            // Act — the registered provider should resolve executable pipelines under the well-known keys
            var pipelineProvider = provider.GetService<ResiliencePipelineProvider<string>>();

            // Assert
            pipelineProvider.ShouldNotBeNull();
            pipelineProvider.GetPipeline(Constants.RetryPipelineName).Execute(() => 42).ShouldBe(42);
            pipelineProvider.GetPipeline(Constants.CircuitBreakerPipelineName).Execute(() => 7).ShouldBe(7);
        }
    }
}
