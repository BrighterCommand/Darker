using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class When_offered_scope_is_from_another_container_should_decline_and_warn_once
    {
        [Fact]
        public async Task When_the_offered_scope_belongs_to_a_container_without_this_darker_should_use_owned_scopes_and_log_one_warning()
        {
            // Arrange — a second container that can resolve ITrackedDependency but has no Darker registration
            var scenario = AmbientProbeScenario.Build();
            var otherServices = new ServiceCollection();
            otherServices.AddScoped<ITrackedDependency, TrackedDependency>();
            otherServices.AddSingleton<DependencyTracker>();
            using var otherProvider = otherServices.BuildServiceProvider();
            var offered = new RecordingAmbientScope(otherProvider.CreateScope());
            var otherDependency = offered.Services.GetRequiredService<ITrackedDependency>();

            // Act
            var results = await scenario.RunConcurrentQueriesAsync(offered);

            // Assert — declined: no query used the other container's instance
            scenario.AssertDeclinedWithOneWarning(results, "from another container", otherDependency);
            offered.DisposeCount.ShouldBe(0);
            offered.DisposeAsyncCount.ShouldBe(0);
            otherDependency.IsDisposed.ShouldBeFalse();
        }
    }
}
