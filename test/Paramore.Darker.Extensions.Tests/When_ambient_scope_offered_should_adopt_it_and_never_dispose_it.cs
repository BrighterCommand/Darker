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
    public class When_ambient_scope_offered_should_adopt_it_and_never_dispose_it
    {
        [Fact]
        public void When_executing_a_query_should_resolve_the_ambient_instance_and_leave_the_ambient_scope_alone()
        {
            // Arrange — Scoped handlers, a provider, and an ambient scope that really disposes its container scope
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

            var ambient = new RecordingAmbientScope(provider.CreateScope());
            var ambientDependency = ambient.Services.GetRequiredService<ITrackedDependency>();
            using var _ = AsyncLocalScopeProvider.Use(ambient);

            // Act
            var result = queryProcessor.Execute(new TrackedQuery());

            // Assert — the handler was given the ambient scope's instance
            ReferenceEquals(result.HandlerDependency, ambientDependency).ShouldBeTrue();

            // Assert — Darker never disposed the ambient scope or what it holds
            ambientDependency.IsDisposed.ShouldBeFalse();
            ReferenceEquals(ambient.Services.GetRequiredService<ITrackedDependency>(), ambientDependency).ShouldBeTrue();
            ambient.DisposeCount.ShouldBe(0);
            ambient.DisposeAsyncCount.ShouldBe(0);

            // Assert — adopting is the configured behaviour, so there is nothing to warn about
            logs.Snapshot().Where(e => e.LogLevel == LogLevel.Warning).ShouldBeEmpty();
        }
    }
}
