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
    public class HostStartThrowOnErrorTests
    {
        [Fact]
        public async Task When_validation_finds_an_error_and_throw_on_error_is_true_should_fail_host_start()
        {
            // Arrange — an unregistered decorator and a duplicate step, validation left at throwOnError = true
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddDarker()
                .AddHandlers(r =>
                {
                    r.Register<UnregisteredDecoratorQuery, UnregisteredDecoratorResult, UnregisteredDecoratorQueryHandler>();
                    r.Register<DuplicateStepQuery, DuplicateStepResult, DuplicateStepQueryHandler>();
                })
                .ValidatePipelines();
            // The host is deliberately not disposed: that would dispose its real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var host = builder.Build();

            // Act — start the host
            var exception = await Should.ThrowAsync<PipelineValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));

            // Assert — the message lists both findings
            exception.Message.ShouldContain("DecoratorResolvable");
            exception.Message.ShouldContain("DuplicateStep");
        }
    }
}
