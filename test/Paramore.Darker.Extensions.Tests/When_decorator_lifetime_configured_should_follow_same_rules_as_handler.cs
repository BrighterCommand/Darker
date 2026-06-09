using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class DecoratorDependencyLifetimeTests
    {
        [Fact]
        public void When_decorator_lifetime_is_singleton_should_reuse_same_dependency_across_queries_and_not_dispose_it()
        {
            // Arrange — Singleton lifetime, with the decorator's injected dependency also Singleton
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: ServiceLifetime.Singleton,
                dependencyLifetime: ServiceLifetime.Singleton);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var tracker = provider.GetRequiredService<DependencyTracker>();

            // Act — execute the same decorated query twice through one processor
            var first = queryProcessor.Execute(new DecoratedTrackedQuery());
            var second = queryProcessor.Execute(new DecoratedTrackedQuery());

            // Assert — the decorator's dependency is constructed once, reused, and never disposed by Darker
            tracker.ConstructionCount.ShouldBe(1);
            ReferenceEquals(first.DecoratorDependency, second.DecoratorDependency).ShouldBeTrue();
            first.DecoratorDependency.IsDisposed.ShouldBeFalse();
            second.DecoratorDependency.IsDisposed.ShouldBeFalse();
        }

        [Fact]
        public async Task When_decorator_lifetime_is_singleton_should_reuse_same_dependency_across_queries_and_not_dispose_it_async()
        {
            // Arrange — Singleton lifetime, with the decorator's injected dependency also Singleton
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: ServiceLifetime.Singleton,
                dependencyLifetime: ServiceLifetime.Singleton);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var tracker = provider.GetRequiredService<DependencyTracker>();

            // Act — execute the same decorated query twice through one processor
            var first = await queryProcessor.ExecuteAsync(new DecoratedTrackedQuery());
            var second = await queryProcessor.ExecuteAsync(new DecoratedTrackedQuery());

            // Assert — the decorator's dependency is constructed once, reused, and never disposed by Darker
            tracker.ConstructionCount.ShouldBe(1);
            ReferenceEquals(first.DecoratorDependency, second.DecoratorDependency).ShouldBeTrue();
            first.DecoratorDependency.IsDisposed.ShouldBeFalse();
            second.DecoratorDependency.IsDisposed.ShouldBeFalse();
        }

        [Fact]
        public void When_decorator_lifetime_is_transient_should_create_fresh_dependency_per_query_and_dispose_after_pipeline()
        {
            // Arrange — Transient lifetime, with the decorator's injected dependency also Transient
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: ServiceLifetime.Transient,
                dependencyLifetime: ServiceLifetime.Transient);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act — execute the same decorated query twice through one processor
            var first = queryProcessor.Execute(new DecoratedTrackedQuery());
            var second = queryProcessor.Execute(new DecoratedTrackedQuery());

            // Assert — a fresh decorator dependency is created per query, and each is disposed after its pipeline
            ReferenceEquals(first.DecoratorDependency, second.DecoratorDependency).ShouldBeFalse();
            first.DecoratorDependency.IsDisposed.ShouldBeTrue();
            second.DecoratorDependency.IsDisposed.ShouldBeTrue();
        }

        [Fact]
        public async Task When_decorator_lifetime_is_transient_should_create_fresh_dependency_per_query_and_dispose_after_pipeline_async()
        {
            // Arrange — Transient lifetime, with the decorator's injected dependency also Transient
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: ServiceLifetime.Transient,
                dependencyLifetime: ServiceLifetime.Transient);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act — execute the same decorated query twice through one processor
            var first = await queryProcessor.ExecuteAsync(new DecoratedTrackedQuery());
            var second = await queryProcessor.ExecuteAsync(new DecoratedTrackedQuery());

            // Assert — a fresh decorator dependency is created per query, and each is disposed after its pipeline
            ReferenceEquals(first.DecoratorDependency, second.DecoratorDependency).ShouldBeFalse();
            first.DecoratorDependency.IsDisposed.ShouldBeTrue();
            second.DecoratorDependency.IsDisposed.ShouldBeTrue();
        }
    }
}
