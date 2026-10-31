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
    public class ValidatePipelinesLastCallWinsTests
    {
        [Fact]
        public async Task When_ValidatePipelines_is_called_twice_should_take_throw_on_error_from_the_last_call()
        {
            // Arrange — an error, validation first told not to throw, then told to throw
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddDarker()
                .AddHandlers(r => r.Register<UnregisteredDecoratorQuery, UnregisteredDecoratorResult, UnregisteredDecoratorQueryHandler>())
                .ValidatePipelines(false)
                .ValidatePipelines();
            // The host is deliberately not disposed: that would dispose its real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var host = builder.Build();

            // Act — start the host
            var exception = await Should.ThrowAsync<PipelineValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));

            // Assert — the last call's throwOnError = true applies
            exception.Message.ShouldContain("DecoratorResolvable");
        }

        [Fact]
        public async Task When_ValidatePipelines_is_called_twice_in_reverse_order_should_let_host_start_succeed()
        {
            // Arrange — an error, validation first told to throw, then told not to
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddDarker()
                .AddHandlers(r => r.Register<UnregisteredDecoratorQuery, UnregisteredDecoratorResult, UnregisteredDecoratorQueryHandler>())
                .ValidatePipelines()
                .ValidatePipelines(false);
            // The host is deliberately not disposed: that would dispose its real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var host = builder.Build();

            // Act — start and stop the host
            await host.StartAsync(TestContext.Current.CancellationToken);
            await host.StopAsync(TestContext.Current.CancellationToken);

            // Assert — start did not throw (reaching here proves it); the host is stopped
            host.Services.ShouldNotBeNull();
        }
    }
}
