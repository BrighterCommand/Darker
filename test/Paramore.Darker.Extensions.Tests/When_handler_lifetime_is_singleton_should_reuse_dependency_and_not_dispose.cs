using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class SingletonHandlerDependencyLifetimeTests
    {
        [Fact]
        public void When_handler_lifetime_is_singleton_should_reuse_same_dependency_across_queries_and_not_dispose_it()
        {
            // Arrange — Singleton handler lifetime, with the injected dependency also Singleton
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: ServiceLifetime.Singleton,
                dependencyLifetime: ServiceLifetime.Singleton);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var tracker = provider.GetRequiredService<DependencyTracker>();

            // Act — execute the same query twice through one processor
            var first = queryProcessor.Execute(new TrackedQuery());
            var second = queryProcessor.Execute(new TrackedQuery());

            // Assert — the dependency is constructed once, reused, and never disposed by Darker
            tracker.ConstructionCount.ShouldBe(1);
            ReferenceEquals(first.HandlerDependency, second.HandlerDependency).ShouldBeTrue();
            first.HandlerDependency.IsDisposed.ShouldBeFalse();
            second.HandlerDependency.IsDisposed.ShouldBeFalse();
        }

        [Fact]
        public async Task When_handler_lifetime_is_singleton_should_reuse_same_dependency_across_queries_and_not_dispose_it_async()
        {
            // Arrange — Singleton handler lifetime, with the injected dependency also Singleton
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: ServiceLifetime.Singleton,
                dependencyLifetime: ServiceLifetime.Singleton);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var tracker = provider.GetRequiredService<DependencyTracker>();

            // Act — execute the same query twice through one processor
            var first = await queryProcessor.ExecuteAsync(new TrackedQuery());
            var second = await queryProcessor.ExecuteAsync(new TrackedQuery());

            // Assert — the dependency is constructed once, reused, and never disposed by Darker
            tracker.ConstructionCount.ShouldBe(1);
            ReferenceEquals(first.HandlerDependency, second.HandlerDependency).ShouldBeTrue();
            first.HandlerDependency.IsDisposed.ShouldBeFalse();
            second.HandlerDependency.IsDisposed.ShouldBeFalse();
        }
    }
}
