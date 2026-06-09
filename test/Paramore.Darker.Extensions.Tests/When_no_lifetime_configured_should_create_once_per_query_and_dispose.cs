using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class DefaultLifetimeDependencyTests
    {
        [Fact]
        public void When_no_lifetime_configured_should_create_dependency_once_per_query_and_dispose_it_after_pipeline()
        {
            // Arrange — no HandlerLifetime configured (default Transient), default Singleton QueryProcessor
            var provider = TrackedDependencyScenario.BuildWithDefaultLifetime(
                dependencyLifetime: ServiceLifetime.Transient);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var tracker = provider.GetRequiredService<DependencyTracker>();

            // Act — execute the same query twice through one processor
            var first = queryProcessor.Execute(new TrackedQuery());
            var second = queryProcessor.Execute(new TrackedQuery());

            // Assert — a fresh dependency is constructed per query, and each is disposed after its pipeline
            tracker.ConstructionCount.ShouldBe(2);
            first.HandlerDependency.IsDisposed.ShouldBeTrue();
            second.HandlerDependency.IsDisposed.ShouldBeTrue();
        }

        [Fact]
        public async Task When_no_lifetime_configured_should_create_dependency_once_per_query_and_dispose_it_after_pipeline_async()
        {
            // Arrange — no HandlerLifetime configured (default Transient), default Singleton QueryProcessor
            var provider = TrackedDependencyScenario.BuildWithDefaultLifetime(
                dependencyLifetime: ServiceLifetime.Transient);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var tracker = provider.GetRequiredService<DependencyTracker>();

            // Act — execute the same query twice through one processor
            var first = await queryProcessor.ExecuteAsync(new TrackedQuery());
            var second = await queryProcessor.ExecuteAsync(new TrackedQuery());

            // Assert — a fresh dependency is constructed per query, and each is disposed after its pipeline
            tracker.ConstructionCount.ShouldBe(2);
            first.HandlerDependency.IsDisposed.ShouldBeTrue();
            second.HandlerDependency.IsDisposed.ShouldBeTrue();
        }
    }
}
