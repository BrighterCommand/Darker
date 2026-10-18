using System.Threading.Tasks;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class OfferedScopeServicesFaultTests
    {
        [Fact]
        public async Task When_the_offered_scope_throws_when_asked_for_its_service_provider_should_use_owned_scopes_and_log_one_warning()
        {
            // Arrange — the ambient scope throws ObjectDisposedException from its Services getter
            var scenario = AmbientProbeScenario.Build();
            var offered = new ThrowingServicesScope();

            // Act
            var results = await scenario.RunConcurrentQueriesAsync(offered);

            // Assert — declined: every query used an owned scope, one Warning naming the exception, and the offered scope was never disposed
            scenario.AssertDeclinedWithOneWarning(results, nameof(System.ObjectDisposedException));
            offered.DisposeCount.ShouldBe(0);
            offered.DisposeAsyncCount.ShouldBe(0);
        }
    }
}
