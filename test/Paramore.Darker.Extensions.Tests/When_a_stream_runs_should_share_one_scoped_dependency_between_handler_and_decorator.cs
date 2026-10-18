using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class When_stream_queries_run_with_scoped_lifetime_should_share_within_and_isolate_between_pipelines
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        [Fact]
        public async Task When_a_stream_runs_should_share_one_scoped_dependency_between_handler_and_decorator()
        {
            // Arrange — Scoped handler lifetime and a Scoped dependency
            var queryProcessor = BuildQueryProcessor();
            var query = new TrackedStreamQuery();

            // Act — enumerate the stream to completion
            query.Release();
            await foreach (var _ in queryProcessor.ExecuteStream(query, cancellationToken: TestContext.Current.CancellationToken)) { }

            // Assert — the handler and the decorator received the same dependency instance
            query.HandlerDependency.ShouldNotBeNull();
            ReferenceEquals(query.HandlerDependency, query.DecoratorDependency).ShouldBeTrue();
        }

        [Fact]
        public async Task When_two_streams_run_concurrently_should_use_isolated_scopes_and_dispose_each_after_its_own_enumeration()
        {
            // Arrange — Scoped handler lifetime and a Scoped dependency, one shared processor
            var queryProcessor = BuildQueryProcessor();
            var queryA = new TrackedStreamQuery();
            var queryB = new TrackedStreamQuery();

            // Act — start enumerating A and wait until its handler is parked, then do the same for B,
            // so both streams are provably in flight together
            var taskA = Task.Run(async () => { await foreach (var _ in queryProcessor.ExecuteStream(queryA, cancellationToken: TestContext.Current.CancellationToken)) { } }, TestContext.Current.CancellationToken);
            await queryA.Started.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            var taskB = Task.Run(async () => { await foreach (var _ in queryProcessor.ExecuteStream(queryB, cancellationToken: TestContext.Current.CancellationToken)) { } }, TestContext.Current.CancellationToken);
            await queryB.Started.WaitAsync(Timeout, TestContext.Current.CancellationToken);

            // Assert — each stream resolved a distinct scoped dependency, and neither is disposed yet
            ReferenceEquals(queryA.HandlerDependency, queryB.HandlerDependency).ShouldBeFalse();
            ReferenceEquals(queryA.DecoratorDependency, queryB.DecoratorDependency).ShouldBeFalse();
            queryA.HandlerDependency.IsDisposed.ShouldBeFalse();
            queryB.HandlerDependency.IsDisposed.ShouldBeFalse();

            // Act — let A's enumeration end while B is still in flight
            queryA.Release();
            await taskA.WaitAsync(Timeout, TestContext.Current.CancellationToken);

            // Assert — A's scope is disposed; B's is untouched
            queryA.HandlerDependency.IsDisposed.ShouldBeTrue();
            queryB.HandlerDependency.IsDisposed.ShouldBeFalse();

            // Act — let B's enumeration end
            queryB.Release();
            await taskB.WaitAsync(Timeout, TestContext.Current.CancellationToken);

            // Assert — B's scope is disposed once its own enumeration ends
            queryB.HandlerDependency.IsDisposed.ShouldBeTrue();
        }

        private static IQueryProcessor BuildQueryProcessor()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<DependencyTracker>();
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddStreamHandlers(registry => registry.Register<TrackedStreamQuery, string, TrackedStreamQueryHandler>())
                .RegisterDecorator(typeof(TrackedStreamDecorator<,>));

            return services.BuildServiceProvider().GetRequiredService<IQueryProcessor>();
        }
    }
}
