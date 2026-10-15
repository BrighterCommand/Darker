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
    public class When_join_ambient_with_transient_handler_lifetime_should_use_owned_scope_and_warn_once
    {
        [Fact]
        public void When_executing_many_queries_should_use_an_owned_scope_each_and_log_one_warning()
        {
            // Arrange — JoinAmbient (the default for AddDarkerScopeProvider), but a Transient handler lifetime
            var logs = new CapturingLoggerProvider();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(logs));
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Transient)
                .AddHandlers(registry => registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandler>());
            services.AddDarkerScopeProvider<AsyncLocalScopeProvider>();

            using var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            using var ambient = new RecordingAmbientScope(provider.CreateScope());
            var ambientDependency = ambient.Services.GetRequiredService<ITrackedDependency>();
            using var _ = AsyncLocalScopeProvider.Use(ambient);

            // Act
            var results = Enumerable.Range(0, 10).Select(__ => queryProcessor.Execute(new TrackedQuery())).ToList();

            // Assert — every query used a scope of its own, not the ambient one, and disposed it
            foreach (var result in results)
            {
                result.HandlerDependency.ShouldNotBeSameAs(ambientDependency);
                result.HandlerDependency.IsDisposed.ShouldBeTrue();
            }

            // Assert — the ambient scope was left alone
            ambientDependency.IsDisposed.ShouldBeFalse();

            // Assert — one Warning across all the queries, naming the reason
            var warnings = logs.Snapshot().Where(e => e.LogLevel == LogLevel.Warning).ToList();
            warnings.Count.ShouldBe(1);
            warnings[0].RenderedMessage.ShouldContain("Scoped handler lifetime");
        }
    }
}
