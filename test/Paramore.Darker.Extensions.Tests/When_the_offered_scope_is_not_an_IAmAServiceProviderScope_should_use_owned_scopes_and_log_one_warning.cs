using System.Threading.Tasks;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class OfferedScopeExposesNoServiceProviderTests
    {
        [Fact]
        public async Task When_the_offered_scope_is_not_an_IAmAServiceProviderScope_should_use_owned_scopes_and_log_one_warning()
        {
            // Arrange — the ambient scope is only an IAmAScope, so Darker cannot resolve anything from it
            var scenario = AmbientProbeScenario.Build();
            var offered = new PlainRecordingScope();

            // Act
            var results = await scenario.RunConcurrentQueriesAsync(offered);

            // Assert — declined: every query used an owned scope, one Warning, and the offered scope was never disposed
            scenario.AssertDeclinedWithOneWarning(results, "exposes no service provider");
            offered.DisposeCount.ShouldBe(0);
            offered.DisposeAsyncCount.ShouldBe(0);
        }
    }
}
