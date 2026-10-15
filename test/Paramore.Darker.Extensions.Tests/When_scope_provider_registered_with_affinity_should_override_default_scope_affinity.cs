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
    public class When_scope_provider_registered_with_affinity_should_override_default_scope_affinity
    {
        // A Transient handler lifetime makes JoinAmbient log the Scoped-lifetime Warning, so the Warning
        // count shows which affinity is effective: one Warning means JoinAmbient, none means AlwaysNew.
        [Theory]
        [InlineData(true, ScopeAffinity.AlwaysNew, ScopeAffinity.JoinAmbient, 1)]
        [InlineData(false, ScopeAffinity.AlwaysNew, ScopeAffinity.JoinAmbient, 1)]
        [InlineData(true, ScopeAffinity.JoinAmbient, ScopeAffinity.AlwaysNew, 0)]
        [InlineData(false, ScopeAffinity.JoinAmbient, ScopeAffinity.AlwaysNew, 0)]
        public void When_the_registered_affinity_differs_from_the_default_should_use_the_registered_affinity(
            bool registeredBeforeAddDarker,
            ScopeAffinity defaultAffinity,
            ScopeAffinity registeredAffinity,
            int expectedWarnings)
        {
            // Arrange
            var logs = new CapturingLoggerProvider();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(logs));
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();

            if (registeredBeforeAddDarker)
                services.AddDarkerScopeProvider<AsyncLocalScopeProvider>(registeredAffinity);

            services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Transient;
                    options.DefaultScopeAffinity = defaultAffinity;
                })
                .AddHandlers(registry => registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandler>());

            if (!registeredBeforeAddDarker)
                services.AddDarkerScopeProvider<AsyncLocalScopeProvider>(registeredAffinity);

            using var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act
            queryProcessor.Execute(new TrackedQuery());

            // Assert — the registered affinity, not the default, decided whether the Warning was logged
            logs.Snapshot().Count(e => e.LogLevel == LogLevel.Warning).ShouldBe(expectedWarnings);
        }
    }
}
