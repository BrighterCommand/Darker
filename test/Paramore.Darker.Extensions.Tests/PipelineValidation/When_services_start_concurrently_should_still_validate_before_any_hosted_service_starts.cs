using System;
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
    public class ConcurrentHostStartValidationTests
    {
        [Fact]
        public async Task When_services_start_concurrently_should_still_validate_before_any_hosted_service_starts()
        {
            // Arrange — concurrent start, and a gated service registered BEFORE ValidatePipelines
            var sequence = new StartupSequence();
            var gated = new GatedRecordingHostedService(sequence);
            var builder = Host.CreateApplicationBuilder();
            builder.Services.Configure<HostOptions>(o => o.ServicesStartConcurrently = true);
            builder.Services.AddSingleton(sequence);
            builder.Services.AddHostedService(_ => gated);
            builder.Services.AddDarker().ValidatePipelines();
            builder.Services.AddSingleton<IAmAPipelineValidator, RecordingValidator>();
            // The host is deliberately not disposed: that would dispose its real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var host = builder.Build();

            // Act — start the host without awaiting it, and wait for the gated service to be entered
            var start = host.StartAsync(TestContext.Current.CancellationToken);
            try
            {
                await gated.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

                // Assert — validation was recorded before the gated service's StartAsync was entered
                gated.ValidationRecordedBeforeEntry.ShouldBeTrue();
            }
            finally
            {
                gated.Release.TrySetResult();
            }

            await start;
            await host.StopAsync(TestContext.Current.CancellationToken);
        }
    }
}
