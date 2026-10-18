using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.Logging;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class When_join_ambient_with_singleton_handler_lifetime_should_not_adopt_and_warn_once
    {
        [Fact]
        public void When_executing_many_queries_should_resolve_from_the_root_and_log_one_warning()
        {
            // Arrange — JoinAmbient (the default for AddDarkerScopeProvider), but a Singleton handler lifetime
            var logs = new CapturingLoggerProvider();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(logs));
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(registry => registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandler>());
            services.AddDarkerScopeProvider<AsyncLocalScopeProvider>();

            // Not disposed: the container's LoggerFactory is now ApplicationLogging.LoggerFactory, and disposing it
            // would break the static loggers of tests that run after this one.
            var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            using var ambient = new RecordingAmbientScope(provider.CreateScope());
            var ambientDependency = ambient.Services.GetRequiredService<ITrackedDependency>();
            using var _ = AsyncLocalScopeProvider.Use(ambient);

            // Act
            var results = Enumerable.Range(0, 5).Select(__ => queryProcessor.Execute(new TrackedQuery())).ToList();

            // Assert — the one Singleton handler holds the root's dependency, never the ambient scope's, and nothing is disposed
            foreach (var result in results)
            {
                result.HandlerDependency.ShouldNotBeSameAs(ambientDependency);
                result.HandlerDependency.ShouldBeSameAs(results[0].HandlerDependency);
                result.HandlerDependency.IsDisposed.ShouldBeFalse();
            }

            ambient.DisposeCount.ShouldBe(0);

            // Assert — one Warning across all the queries, naming the reason
            var warnings = logs.Snapshot().Where(e => e.LogLevel == LogLevel.Warning).ToList();
            warnings.Count.ShouldBe(1);
            warnings[0].RenderedMessage.ShouldContain("Scoped handler lifetime");
        }
    }
}
