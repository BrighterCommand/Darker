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
    public class LifetimeValidationSideEffectTests
    {
        [Fact]
        public async Task When_lifetime_validation_runs_should_create_no_handler_or_scope_provider()
        {
            // Arrange — a Scoped, JoinAmbient configuration with a registered scope provider and a handler, each counting its construction
            var counter = new ConstructionCounter();
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton(counter);
            builder.Services.AddSingleton<IAmAScopeProvider, ConstructionRecordingScopeProvider>();
            var darker = builder.Services.AddDarker(o =>
                {
                    o.HandlerLifetime = ServiceLifetime.Scoped;
                    o.DefaultScopeAffinity = ScopeAffinity.JoinAmbient;
                })
                .AddHandlers(r => r.Register<ConstructionRecordingQuery, ConstructionRecordingResult, ConstructionRecordingHandler>());
            darker.RegisterDecorator(typeof(ConstructionRecordingDecorator<,>));
            darker.ValidatePipelines();
            // Not disposed: AddDarker stores the real ILoggerFactory in a static, and disposing it would break later tests.
            var host = builder.Build();

            // Act — start the host (which validates) and validate again explicitly
            await host.StartAsync(TestContext.Current.CancellationToken);
            host.Services.ValidateAll();

            // Assert — lifetime validation only read registrations: neither handler nor scope provider was constructed
            counter.Handlers.ShouldBe(0);
            counter.ScopeProviders.ShouldBe(0);

            await host.StopAsync(TestContext.Current.CancellationToken);
        }
    }
}
