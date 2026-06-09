using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class PipelineFailureDisposalTests
    {
        [Theory]
        [InlineData(ServiceLifetime.Transient)]
        [InlineData(ServiceLifetime.Scoped)]
        public void When_handler_throws_should_still_dispose_the_per_query_dependency(ServiceLifetime lifetime)
        {
            // Arrange — a handler that records its per-query dependency then throws
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: lifetime,
                dependencyLifetime: lifetime);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var query = new ThrowingTrackedQuery();

            // Act — the pipeline throws
            Should.Throw<InvalidOperationException>(() => queryProcessor.Execute(query));

            // Assert — the per-query dependency is disposed despite the failure
            query.HandlerDependency.IsDisposed.ShouldBeTrue();
        }

        [Theory]
        [InlineData(ServiceLifetime.Transient)]
        [InlineData(ServiceLifetime.Scoped)]
        public async Task When_async_handler_throws_should_still_dispose_the_per_query_dependency(ServiceLifetime lifetime)
        {
            // Arrange — an async handler that records its per-query dependency then throws
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: lifetime,
                dependencyLifetime: lifetime);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var query = new ThrowingTrackedQuery();

            // Act — the pipeline throws
            await Should.ThrowAsync<InvalidOperationException>(() => queryProcessor.ExecuteAsync(query));

            // Assert — the per-query dependency is disposed despite the failure
            query.HandlerDependency.IsDisposed.ShouldBeTrue();
        }

        [Theory]
        [InlineData(ServiceLifetime.Transient)]
        [InlineData(ServiceLifetime.Scoped)]
        public async Task When_async_handler_is_cancelled_should_still_dispose_the_per_query_dependency(ServiceLifetime lifetime)
        {
            // Arrange — an async handler that records its per-query dependency then observes cancellation
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: lifetime,
                dependencyLifetime: lifetime);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var query = new CancellingTrackedQuery();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act — the pipeline is cancelled
            await Should.ThrowAsync<OperationCanceledException>(
                () => queryProcessor.ExecuteAsync(query, cancellationToken: cts.Token));

            // Assert — the per-query dependency is disposed despite the cancellation
            query.HandlerDependency.IsDisposed.ShouldBeTrue();
        }
    }
}
