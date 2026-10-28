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
    public class HostStartValidationTests
    {
        [Fact]
        public async Task When_the_host_starts_should_validate_before_any_hosted_service_starts()
        {
            // Arrange — another hosted service registered BEFORE ValidatePipelines, and a validator recording into the same sequence
            var sequence = new StartupSequence();
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton(sequence);
            builder.Services.AddHostedService<RecordingHostedService>();
            builder.Services.AddDarker().ValidatePipelines();
            builder.Services.AddSingleton<IAmAPipelineValidator, RecordingValidator>();
            // The host is deliberately not disposed: that would dispose its real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var host = builder.Build();

            // Act — start the host
            await host.StartAsync(TestContext.Current.CancellationToken);

            // Assert — validation ran in the starting phase, ahead of the other service's StartAsync
            sequence.Entries.ShouldBe(new[] { "validation", "other service StartAsync" });

            await host.StopAsync(TestContext.Current.CancellationToken);
        }
    }
}
