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
    public class JoinAmbientWithoutScopeProviderTests
    {
        [Fact]
        public void When_executing_many_queries_without_a_scope_provider_should_use_an_owned_scope_each_and_log_one_warning()
        {
            // Arrange — JoinAmbient and a Scoped handler lifetime, but no scope provider is registered
            var logs = new CapturingLoggerProvider();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(logs));
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Scoped;
                    options.DefaultScopeAffinity = ScopeAffinity.JoinAmbient;
                })
                .AddHandlers(registry => registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandler>());

            // Not disposed: the container's LoggerFactory is now ApplicationLogging.LoggerFactory, and disposing it
            // would break the static loggers of tests that run after this one.
            var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act
            var results = Enumerable.Range(0, 10).Select(_ => queryProcessor.Execute(new TrackedQuery())).ToList();

            // Assert — every query used a scope of its own and disposed it
            results.Select(r => r.HandlerDependency).Distinct().Count().ShouldBe(10);
            foreach (var result in results)
                result.HandlerDependency.IsDisposed.ShouldBeTrue();

            // Assert — one Warning across all the queries, naming the missing provider
            var warnings = logs.Snapshot().Where(e => e.LogLevel == LogLevel.Warning).ToList();
            warnings.Count.ShouldBe(1);
            warnings[0].RenderedMessage.ShouldContain("no scope provider is registered");
        }
    }
}
