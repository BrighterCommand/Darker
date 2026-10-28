using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    [Collection("DarkerHostBootstrap")]
    public class ValidateOnceGuardTests
    {
        [Fact]
        public async Task When_start_async_is_called_twice_should_validate_once()
        {
            // Arrange — a counting validator beside ValidatePipelines, and a host that never calls StartingAsync
            var counting = new CountingValidator();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDarker().ValidatePipelines();
            services.AddSingleton<IAmAPipelineValidator>(counting);
            // Deliberately not disposed: that would dispose the real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var provider = services.BuildServiceProvider();

            // Act — call only StartAsync, twice, on each hosted service
            foreach (var hosted in provider.GetServices<IHostedService>())
            {
                await hosted.StartAsync(TestContext.Current.CancellationToken);
                await hosted.StartAsync(TestContext.Current.CancellationToken);
            }

            // Assert — validation ran exactly once
            counting.Count.ShouldBe(1);
        }

        [Fact]
        public async Task When_the_host_runs_both_phases_should_validate_once()
        {
            // Arrange — a real host with a counting validator beside ValidatePipelines
            var counting = new CountingValidator();
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddDarker().ValidatePipelines();
            builder.Services.AddSingleton<IAmAPipelineValidator>(counting);
            // Deliberately not disposed: that would dispose the real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var host = builder.Build();

            // Act — run StartingAsync and StartAsync via the host, then stop
            await host.StartAsync(TestContext.Current.CancellationToken);
            await host.StopAsync(TestContext.Current.CancellationToken);

            // Assert — validation ran exactly once
            counting.Count.ShouldBe(1);
        }
    }
}
