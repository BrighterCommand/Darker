using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class ConcurrentScopeIsolationLifetimeTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        [Fact]
        public void When_two_queries_run_concurrently_should_use_isolated_scopes_and_not_dispose_each_others_dependencies()
        {
            // Arrange — Scoped handler + Scoped dependency, one shared Singleton processor
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: ServiceLifetime.Scoped,
                dependencyLifetime: ServiceLifetime.Scoped);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var queryA = new ConcurrentTrackedQuery();
            var queryB = new ConcurrentTrackedQuery();

            // Act — start both pipelines and wait until both handlers are provably in flight
            var taskA = Task.Run(() => queryProcessor.Execute(queryA));
            var taskB = Task.Run(() => queryProcessor.Execute(queryB));
            Task.WaitAll(new[] { queryA.Started, queryB.Started }, Timeout).ShouldBeTrue();

            // Assert — each pipeline resolved a distinct scoped dependency (isolated scopes)
            ReferenceEquals(queryA.HandlerDependency, queryB.HandlerDependency).ShouldBeFalse();

            // Act — let A complete while B is still in flight
            queryA.Release();
            taskA.Wait(Timeout).ShouldBeTrue();

            // Assert — A's scope is disposed; B's is untouched (neither disposes the other's scope)
            queryA.HandlerDependency.IsDisposed.ShouldBeTrue();
            queryB.HandlerDependency.IsDisposed.ShouldBeFalse();

            // Cleanup — release B
            queryB.Release();
            taskB.Wait(Timeout).ShouldBeTrue();
        }

        [Fact]
        public async Task When_two_queries_run_concurrently_should_use_isolated_scopes_and_not_dispose_each_others_dependencies_async()
        {
            // Arrange — Scoped handler + Scoped dependency, one shared Singleton processor
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: ServiceLifetime.Scoped,
                dependencyLifetime: ServiceLifetime.Scoped);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var queryA = new ConcurrentTrackedQuery();
            var queryB = new ConcurrentTrackedQuery();

            // Act — start both pipelines and wait until both handlers are provably in flight
            var taskA = queryProcessor.ExecuteAsync(queryA);
            var taskB = queryProcessor.ExecuteAsync(queryB);
            await Task.WhenAll(queryA.Started, queryB.Started).WaitAsync(Timeout);

            // Assert — each pipeline resolved a distinct scoped dependency (isolated scopes)
            ReferenceEquals(queryA.HandlerDependency, queryB.HandlerDependency).ShouldBeFalse();

            // Act — let A complete while B is still in flight
            queryA.Release();
            await taskA.WaitAsync(Timeout);

            // Assert — A's scope is disposed; B's is untouched (neither disposes the other's scope)
            queryA.HandlerDependency.IsDisposed.ShouldBeTrue();
            queryB.HandlerDependency.IsDisposed.ShouldBeFalse();

            // Cleanup — release B
            queryB.Release();
            await taskB.WaitAsync(Timeout);
        }
    }
}
