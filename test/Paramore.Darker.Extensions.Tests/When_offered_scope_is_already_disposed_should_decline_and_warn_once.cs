using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class When_offered_scope_is_already_disposed_should_decline_and_warn_once
    {
        [Fact]
        public async Task When_the_ambient_scope_was_disposed_before_the_queries_should_use_owned_scopes_and_log_one_warning()
        {
            // Arrange — the owner of the ambient scope has already disposed it
            var scenario = AmbientProbeScenario.Build();
            var underlyingScope = scenario.Provider.CreateScope();
            var offered = new RecordingAmbientScope(underlyingScope);
            underlyingScope.Dispose();

            // Act
            var results = await scenario.RunConcurrentQueriesAsync(offered);

            // Assert — declined: Darker used owned scopes, named the exception, and did not touch the offered scope
            scenario.AssertDeclinedWithOneWarning(results, nameof(System.ObjectDisposedException));
            offered.DisposeCount.ShouldBe(0);
            offered.DisposeAsyncCount.ShouldBe(0);
        }
    }
}
