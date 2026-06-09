using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class ScopedDependencySharingLifetimeTests
    {
        [Fact]
        public void When_handler_lifetime_is_scoped_should_share_one_scoped_dependency_across_handler_and_decorator_then_dispose()
        {
            // Arrange — Scoped handler lifetime, with the injected dependency also Scoped, on a query
            // whose handler and decorator both receive ITrackedDependency by constructor injection
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: ServiceLifetime.Scoped,
                dependencyLifetime: ServiceLifetime.Scoped);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var tracker = provider.GetRequiredService<DependencyTracker>();

            // Act — execute the decorated query once through one processor
            var result = queryProcessor.Execute(new DecoratedTrackedQuery());

            // Assert — handler and decorator share one scoped dependency, constructed once, disposed after
            tracker.ConstructionCount.ShouldBe(1);
            ReferenceEquals(result.HandlerDependency, result.DecoratorDependency).ShouldBeTrue();
            result.HandlerDependency.IsDisposed.ShouldBeTrue();
        }

        [Fact]
        public async Task When_handler_lifetime_is_scoped_should_share_one_scoped_dependency_across_handler_and_decorator_then_dispose_async()
        {
            // Arrange — Scoped handler lifetime, with the injected dependency also Scoped, on a query
            // whose handler and decorator both receive ITrackedDependency by constructor injection
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: ServiceLifetime.Scoped,
                dependencyLifetime: ServiceLifetime.Scoped);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var tracker = provider.GetRequiredService<DependencyTracker>();

            // Act — execute the decorated query once through one processor
            var result = await queryProcessor.ExecuteAsync(new DecoratedTrackedQuery());

            // Assert — handler and decorator share one scoped dependency, constructed once, disposed after
            tracker.ConstructionCount.ShouldBe(1);
            ReferenceEquals(result.HandlerDependency, result.DecoratorDependency).ShouldBeTrue();
            result.HandlerDependency.IsDisposed.ShouldBeTrue();
        }
    }
}
