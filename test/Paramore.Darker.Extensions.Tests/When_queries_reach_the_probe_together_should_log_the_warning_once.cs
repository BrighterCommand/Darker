using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class WarningLatchPerContainerTests
    {
        private const int CONCURRENT_QUERIES = 20;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        [Fact]
        public async Task When_queries_reach_the_probe_together_should_log_the_warning_once()
        {
            // Arrange — a provider that holds all 20 queries at its gate, then offers each the root scope
            var gated = new GatedScopeProvider(CONCURRENT_QUERIES);
            var scenario = AmbientProbeScenario.Build(configure: services => services.AddSingleton<IAmAScopeProvider>(gated));
            gated.Offered = new FixedServicesScope(scenario.Provider);
            var queryProcessor = scenario.Provider.GetRequiredService<IQueryProcessor>();

            // Act — start every query on a thread of its own, let them all arrive, then release them together
            var queries = Enumerable.Range(0, CONCURRENT_QUERIES)
                .Select(_ => Task.Factory.StartNew(
                    () => queryProcessor.Execute(new TrackedQuery()),
                    CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default))
                .ToList();
            gated.WaitForAllToArrive(Timeout).ShouldBeTrue();
            gated.Release();
            var results = await Task.WhenAll(queries);

            // Assert
            scenario.AssertDeclinedWithOneWarning(results, "is the root");
        }

        [Fact]
        public async Task When_two_different_reasons_recur_in_one_container_should_log_one_warning_for_each()
        {
            // Arrange — some queries are offered the root scope, others a scope with no service provider
            var scenario = AmbientProbeScenario.Build();

            // Act
            await scenario.RunConcurrentQueriesAsync(new FixedServicesScope(scenario.Provider));
            await scenario.RunConcurrentQueriesAsync(new PlainRecordingScope());

            // Assert — one Warning per reason
            var warnings = scenario.Logs.Snapshot().Where(e => e.LogLevel == LogLevel.Warning).ToList();
            warnings.Count.ShouldBe(2);
            warnings.Count(w => w.RenderedMessage.Contains("is the root")).ShouldBe(1);
            warnings.Count(w => w.RenderedMessage.Contains("exposes no service provider")).ShouldBe(1);
        }

        [Fact]
        public void When_a_scoped_processor_builds_a_factory_for_each_request_should_log_the_warning_once()
        {
            // Arrange — a Scoped QueryProcessor means every request scope builds a new factory
            var scenario = AmbientProbeScenario.Build(queryProcessorLifetime: ServiceLifetime.Scoped);
            using var _ = AsyncLocalScopeProvider.Use(new FixedServicesScope(scenario.Provider));

            // Act — five requests, each with its own scope, so five factories
            for (var request = 0; request < 5; request++)
            {
                using var requestScope = scenario.Provider.CreateScope();
                requestScope.ServiceProvider.GetRequiredService<IQueryProcessor>().Execute(new TrackedQuery());
            }

            // Assert
            scenario.Logs.Snapshot().Count(e => e.LogLevel == LogLevel.Warning).ShouldBe(1);
        }

        [Fact]
        public async Task When_two_containers_share_the_misconfiguration_should_log_one_warning_in_each()
        {
            // Arrange / Act — two separately built containers, each misconfigured the same way
            var first = AmbientProbeScenario.Build();
            await first.RunConcurrentQueriesAsync(new FixedServicesScope(first.Provider));

            var second = AmbientProbeScenario.Build();
            await second.RunConcurrentQueriesAsync(new FixedServicesScope(second.Provider));

            // Assert — the latch belongs to the container, so neither silences the other
            first.Logs.Snapshot().Count(e => e.LogLevel == LogLevel.Warning).ShouldBe(1);
            second.Logs.Snapshot().Count(e => e.LogLevel == LogLevel.Warning).ShouldBe(1);
        }
    }
}
