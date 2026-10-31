using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    [Collection("DarkerHostBootstrap")]
    public class ValidationSideEffectTests
    {
        [Fact]
        public async Task When_validation_runs_should_create_no_handler_or_decorator()
        {
            // Arrange — a decorated handler and its decorator, each counting its own construction
            var counter = new ConstructionCounter();
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton(counter);
            var darker = builder.Services.AddDarker()
                .AddHandlers(r => r.Register<ConstructionRecordingQuery, ConstructionRecordingResult, ConstructionRecordingHandler>());
            darker.RegisterDecorator(typeof(ConstructionRecordingDecorator<,>));
            darker.ValidatePipelines();
            // Not disposed: AddDarker stores the real ILoggerFactory in a static, and disposing it would break later tests.
            var host = builder.Build();

            // Act — start the host (which validates) and validate again explicitly
            await host.StartAsync(TestContext.Current.CancellationToken);
            var result = host.Services.ValidateAll();

            // Assert — validation only read registrations: nothing was constructed
            counter.Handlers.ShouldBe(0);
            counter.Decorators.ShouldBe(0);
            result.IsValid.ShouldBeTrue();

            await host.StopAsync(TestContext.Current.CancellationToken);
        }
    }
}
