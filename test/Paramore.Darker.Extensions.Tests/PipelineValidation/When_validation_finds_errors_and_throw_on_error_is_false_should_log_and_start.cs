using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    public class HostStartLogOnErrorTests
    {
        [Fact]
        public async Task When_validation_finds_errors_and_throw_on_error_is_false_should_log_and_start()
        {
            // Arrange — an unregistered decorator and a removed handler, with throwOnError = false
            var builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders();
            var capture = new CapturingLoggerProvider();
            builder.Logging.AddProvider(capture);
            builder.Services.AddDarker()
                .AddHandlers(r =>
                {
                    r.Register<UnregisteredDecoratorQuery, UnregisteredDecoratorResult, UnregisteredDecoratorQueryHandler>();
                    r.Register<RemovedHandlerQuery, RemovedHandlerResult, RemovedHandlerQueryHandler>();
                })
                .ValidatePipelines(throwOnError: false);
            builder.Services.RemoveAll<RemovedHandlerQueryHandler>();
            // The host is deliberately not disposed: that would dispose its real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var host = builder.Build();

            // Act — start and stop the host
            await host.StartAsync(TestContext.Current.CancellationToken);
            await host.StopAsync(TestContext.Current.CancellationToken);

            // Assert — one Error entry per finding, each carrying its rule and source
            var errors = capture.Snapshot().Where(e => e.LogLevel == LogLevel.Error).ToList();
            errors.Count.ShouldBe(2);
            errors.ShouldContain(e => e.RenderedMessage.Contains("DecoratorResolvable")
                && e.RenderedMessage.Contains(nameof(UnregisteredDecoratorQueryHandler)));
            errors.ShouldContain(e => e.RenderedMessage.Contains("HandlerResolvable")
                && e.RenderedMessage.Contains(nameof(RemovedHandlerQueryHandler)));
        }
    }
}
