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
    public class When_scope_provider_offers_no_scope_should_use_owned_scope_without_warning
    {
        [Fact]
        public void When_no_scope_is_ambient_should_use_an_owned_scope_each_query_and_log_no_warning()
        {
            // Arrange — Scoped handlers and a provider, but nothing has made a scope ambient
            var logs = new CapturingLoggerProvider();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(logs));
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry => registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandler>());
            services.AddDarkerScopeProvider<AsyncLocalScopeProvider>();

            using var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act
            var results = Enumerable.Range(0, 3).Select(_ => queryProcessor.Execute(new TrackedQuery())).ToList();

            // Assert — each query had a scope of its own and disposed it
            results.Select(r => r.HandlerDependency).Distinct().Count().ShouldBe(3);
            foreach (var result in results)
                result.HandlerDependency.IsDisposed.ShouldBeTrue();

            // Assert — no ambient scope is the normal answer outside a request, so nothing is logged
            var warnings = logs.Snapshot().Where(e => e.LogLevel == LogLevel.Warning).ToList();
            warnings.ShouldBeEmpty();
        }
    }
}
