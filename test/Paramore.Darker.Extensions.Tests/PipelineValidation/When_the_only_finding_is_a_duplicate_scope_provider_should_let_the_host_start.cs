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
    public class HostStartDuplicateScopeProviderTests
    {
        private const string HOSTED_SERVICE_CATEGORY = "Paramore.Darker.Extensions.DependencyInjection.DarkerValidationHostedService";

        [Fact]
        public async Task When_the_only_finding_is_a_duplicate_scope_provider_should_let_the_host_start()
        {
            // Arrange — a clean handler and two different unkeyed scope providers, throwOnError = true
            var capture = new CapturingLoggerProvider();
            var builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            builder.Logging.AddProvider(capture);
            builder.Logging.AddFilter<CapturingLoggerProvider>((category, _) => category == HOSTED_SERVICE_CATEGORY);
            builder.Services.AddSingleton<IAmAScopeProvider, TestScopeProvider>();
            builder.Services.AddSingleton<IAmAScopeProvider, AsyncLocalScopeProvider>();
            builder.Services.AddDarker()
                .AddHandlers(r => r.Register<RemovedHandlerQuery, RemovedHandlerResult, RemovedHandlerQueryHandler>())
                .ValidatePipelines(throwOnError: true);
            // The host is deliberately not disposed: that would dispose its real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var host = builder.Build();

            // Act — start and stop the host
            await host.StartAsync(TestContext.Current.CancellationToken);
            await host.StopAsync(TestContext.Current.CancellationToken);

            // Assert — the host started and the single Warning names the rule
            var warnings = capture.Snapshot().Where(e => e.LogLevel == LogLevel.Warning).ToList();
            warnings.Count.ShouldBe(1);
            warnings[0].RenderedMessage.ShouldContain("DuplicateScopeProvider");
        }
    }
}
