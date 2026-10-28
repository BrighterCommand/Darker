using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.Logging;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    [Collection("DarkerHostBootstrap")]
    public class HostStartLogOnWarningTests
    {
        private const string HOSTED_SERVICE_CATEGORY = "Paramore.Darker.Extensions.DependencyInjection.DarkerValidationHostedService";

        [Fact]
        public async Task When_validation_finds_only_warnings_should_log_them_and_start()
        {
            // Arrange — a duplicate-step handler (a warning) with its decorator registered, throwOnError = true
            var capture = new CapturingLoggerProvider();
            var builder = CreateBuilder(capture);
            var darker = builder.Services.AddDarker()
                .AddHandlers(r => r.Register<DuplicateStepQuery, DuplicateStepResult, DuplicateStepQueryHandler>());
            darker.RegisterDecorator(typeof(InstanceRecordingDecorator<,>));
            darker.ValidatePipelines(throwOnError: true);
            // The host is deliberately not disposed: that would dispose its real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var host = builder.Build();

            // Act — start and stop the host
            await host.StartAsync(TestContext.Current.CancellationToken);
            await host.StopAsync(TestContext.Current.CancellationToken);

            // Assert — one Warning entry naming the rule, and the host started
            var warnings = capture.Snapshot().Where(e => e.LogLevel == LogLevel.Warning).ToList();
            warnings.Count.ShouldBe(1);
            warnings[0].RenderedMessage.ShouldContain("DuplicateStep");
        }

        [Fact]
        public async Task When_validation_finds_nothing_should_log_only_a_debug_summary()
        {
            // Arrange — a clean configuration of a public, decorator-free handler
            var capture = new CapturingLoggerProvider();
            var builder = CreateBuilder(capture);
            builder.Services.AddDarker()
                .AddHandlers(r => r.Register<RemovedHandlerQuery, RemovedHandlerResult, RemovedHandlerQueryHandler>())
                .ValidatePipelines();
            // The host is deliberately not disposed: that would dispose its real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var host = builder.Build();

            // Act — start and stop the host
            await host.StartAsync(TestContext.Current.CancellationToken);
            await host.StopAsync(TestContext.Current.CancellationToken);

            // Assert — nothing above Debug, and exactly one Debug summary
            var entries = capture.Snapshot();
            entries.ShouldNotContain(e => e.LogLevel > LogLevel.Debug);
            entries.Count(e => e.LogLevel == LogLevel.Debug).ShouldBe(1);
        }

        private static HostApplicationBuilder CreateBuilder(CapturingLoggerProvider capture)
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            builder.Logging.AddProvider(capture);
            builder.Logging.AddFilter<CapturingLoggerProvider>((category, _) => category == HOSTED_SERVICE_CATEGORY);
            return builder;
        }
    }
}
