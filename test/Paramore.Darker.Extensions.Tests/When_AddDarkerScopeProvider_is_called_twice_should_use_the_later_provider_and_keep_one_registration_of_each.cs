using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class When_scope_provider_registered_twice_should_use_the_later_registration
    {
        [Fact]
        public void When_AddDarkerScopeProvider_is_called_twice_should_use_the_later_provider_and_keep_one_registration_of_each()
        {
            // Arrange — the first provider would throw if it were ever asked; the later one offers the ambient scope
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry => registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandler>());
            services.AddDarkerScopeProvider<ThrowingScopeProvider>(ScopeAffinity.JoinAmbient);
            services.AddDarkerScopeProvider<AsyncLocalScopeProvider>(ScopeAffinity.JoinAmbient);

            using var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var ambient = new RecordingAmbientScope(provider.CreateScope());
            var ambientDependency = ambient.Services.GetRequiredService<ITrackedDependency>();
            using var _ = AsyncLocalScopeProvider.Use(ambient);

            // Act
            TrackedQuery.Result result = null;
            Should.NotThrow(() => result = queryProcessor.Execute(new TrackedQuery()));

            // Assert — the later provider won: the query adopted the ambient scope
            ReferenceEquals(result.HandlerDependency, ambientDependency).ShouldBeTrue();

            // Assert — the later registration replaced the earlier one rather than joining it
            services.Count(d => d.ServiceType == typeof(IAmAScopeProvider)).ShouldBe(1);
            services.Count(d => d.ServiceType.Name == "ScopeAffinityRegistration").ShouldBe(1);
        }
    }
}
