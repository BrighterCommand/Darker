using System;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class When_borrowed_scope_disposed_after_probe_should_surface_ObjectDisposedException_unwrapped
    {
        [Fact]
        public void When_the_owner_disposes_the_scope_while_the_pipeline_is_built_should_surface_ObjectDisposedException()
        {
            // Arrange — the handler's constructor has the ambient scope disposed, so its decorator cannot be resolved
            using var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            using var _ = MakeAmbient(provider);

            // Act
            var exception = Record.Exception(() => queryProcessor.Execute(new ScopeDisposingQuery()));

            // Assert — the container's own exception reaches the caller, not a Darker wrapper
            exception.ShouldBeOfType<ObjectDisposedException>();
        }

        [Fact]
        public void When_the_owner_disposes_the_scope_during_the_query_should_surface_ObjectDisposedException_from_the_users_service_use()
        {
            // Arrange — the handler has the ambient scope disposed, then resolves from the provider it was given
            using var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            using var _ = MakeAmbient(provider);

            // Act
            var exception = Record.Exception(() => queryProcessor.Execute(new ScopeDisposingResolvingQuery()));

            // Assert
            exception.ShouldBeOfType<ObjectDisposedException>();
        }

        private static IDisposable MakeAmbient(ServiceProvider provider)
        {
            var scope = provider.CreateScope();
            provider.GetRequiredService<AmbientScopeOwner>().Scope = scope;
            return AsyncLocalScopeProvider.Use(new RecordingAmbientScope(scope));
        }

        private static ServiceProvider Build()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddSingleton<AmbientScopeOwner>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry =>
                {
                    registry.Register<ScopeDisposingQuery, SharedRecordingResult, ScopeDisposingHandler>();
                    registry.Register<ScopeDisposingResolvingQuery, SharedRecordingResult, ScopeDisposingResolvingHandler>();
                })
                .RegisterDecorator(typeof(InstanceRecordingDecorator<,>));
            services.AddDarkerScopeProvider<AsyncLocalScopeProvider>();

            return services.BuildServiceProvider();
        }
    }
}
