using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.Logging;
using Shouldly;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// The shared shape of the ambient scope probe tests: Scoped handlers behind an
    /// <see cref="AsyncLocalScopeProvider"/>, many concurrent queries run while a given scope is ambient,
    /// and the checks that every query declined it, used an owned scope and logged one Warning.
    /// </summary>
    public sealed class AmbientProbeScenario
    {
        private const int QUERY_COUNT = 20;

        public AmbientProbeScenario(CapturingLoggerProvider logs, ServiceProvider provider)
        {
            Logs = logs;
            Provider = provider;
        }

        public CapturingLoggerProvider Logs { get; }

        public ServiceProvider Provider { get; }

        public static AmbientProbeScenario Build(
            ServiceLifetime queryProcessorLifetime = ServiceLifetime.Singleton,
            System.Action<IServiceCollection> configure = null)
        {
            var logs = new CapturingLoggerProvider();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(logs));
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Scoped;
                    options.QueryProcessorLifetime = queryProcessorLifetime;
                })
                .AddHandlers(registry => registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandler>());
            services.AddDarkerScopeProvider<AsyncLocalScopeProvider>();
            configure?.Invoke(services);

            return new AmbientProbeScenario(logs, services.BuildServiceProvider());
        }

        /// <summary>Runs many queries at once, each seeing <paramref name="offered"/> as the ambient scope.</summary>
        public async Task<IReadOnlyList<TrackedQuery.Result>> RunConcurrentQueriesAsync(IAmAScope offered)
        {
            var queryProcessor = Provider.GetRequiredService<IQueryProcessor>();
            using var _ = AsyncLocalScopeProvider.Use(offered);

            var tasks = Enumerable.Range(0, QUERY_COUNT)
                .Select(__ => Task.Run(() => queryProcessor.Execute(new TrackedQuery())));

            return await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Every query used an owned scope (its dependency is its own and was disposed), and exactly one
        /// Warning names the reason.
        /// </summary>
        public void AssertDeclinedWithOneWarning(
            IReadOnlyList<TrackedQuery.Result> results,
            string expectedReason,
            ITrackedDependency ambientDependency = null)
        {
            results.Count.ShouldBe(QUERY_COUNT);
            results.Select(r => r.HandlerDependency).Distinct().Count().ShouldBe(QUERY_COUNT);

            foreach (var result in results)
            {
                result.HandlerDependency.IsDisposed.ShouldBeTrue();
                if (ambientDependency != null)
                    ReferenceEquals(result.HandlerDependency, ambientDependency).ShouldBeFalse();
            }

            var warnings = Logs.Snapshot().Where(e => e.LogLevel == LogLevel.Warning).ToList();
            warnings.Count.ShouldBe(1);
            warnings[0].RenderedMessage.ShouldContain(expectedReason);
        }
    }
}
