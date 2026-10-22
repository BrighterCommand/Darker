using System.Threading.Tasks;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class OfferedScopeHasNullServicesTests
    {
        [Fact]
        public async Task When_the_offered_scope_has_no_service_provider_should_use_owned_scopes_and_log_one_warning()
        {
            // Arrange — the ambient scope claims to expose a service provider but returns null
            var scenario = AmbientProbeScenario.Build();
            var offered = new NullServicesScope();

            // Act
            var results = await scenario.RunConcurrentQueriesAsync(offered);

            // Assert — declined: every query used an owned scope, one Warning, and the offered scope was never disposed
            scenario.AssertDeclinedWithOneWarning(results, "has no service provider");
            offered.DisposeCount.ShouldBe(0);
            offered.DisposeAsyncCount.ShouldBe(0);
        }
    }
}
