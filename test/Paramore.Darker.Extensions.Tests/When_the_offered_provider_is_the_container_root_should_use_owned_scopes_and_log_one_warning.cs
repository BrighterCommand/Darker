using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class When_offered_scope_is_the_root_provider_should_decline_and_warn_once
    {
        [Fact]
        public async Task When_the_offered_provider_is_the_container_root_should_use_owned_scopes_and_log_one_warning()
        {
            // Arrange — the offered scope exposes the ServiceProvider that BuildServiceProvider returned
            var scenario = AmbientProbeScenario.Build();
            var offered = new FixedServicesScope(scenario.Provider);

            // Act
            var results = await scenario.RunConcurrentQueriesAsync(offered);

            // Assert — declined: resolving Scoped services from the root would leak them for the life of the container
            scenario.AssertDeclinedWithOneWarning(results, "is the root");
            offered.DisposeCount.ShouldBe(0);
            offered.DisposeAsyncCount.ShouldBe(0);
        }

        [Fact]
        public async Task When_the_offered_provider_is_the_root_captured_by_a_singleton_factory_should_use_owned_scopes_and_log_one_warning()
        {
            // Arrange — the root as a singleton factory delegate receives it, not as BuildServiceProvider returns it
            var scenario = AmbientProbeScenario.Build(configure: services =>
                services.AddSingleton(serviceProvider => new RootCapture(serviceProvider)));
            var offered = new FixedServicesScope(scenario.Provider.GetRequiredService<RootCapture>().Services);

            // Act
            var results = await scenario.RunConcurrentQueriesAsync(offered);

            // Assert
            scenario.AssertDeclinedWithOneWarning(results, "is the root");
            offered.DisposeCount.ShouldBe(0);
            offered.DisposeAsyncCount.ShouldBe(0);
        }
    }
}
