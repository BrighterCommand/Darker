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
    public class AffinityIsAlwaysNewTests
    {
        [Fact]
        public void When_a_throwing_scope_provider_is_registered_should_run_the_query_with_an_owned_scope()
        {
            // Arrange
            var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var scopeProvider = (ThrowingScopeProvider)provider.GetRequiredService<IAmAScopeProvider>();

            // Act
            TrackedQuery.Result result = null;
            Should.NotThrow(() => result = queryProcessor.Execute(new TrackedQuery()));

            // Assert — the Scoped dependency is disposed after the query, so the scope was owned; the provider was never asked
            result.HandlerDependency.IsDisposed.ShouldBeTrue();
            scopeProvider.CallCount.ShouldBe(0);
        }

        [Fact]
        public async Task When_a_throwing_scope_provider_is_registered_should_run_the_async_query_with_an_owned_scope()
        {
            // Arrange
            var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var scopeProvider = (ThrowingScopeProvider)provider.GetRequiredService<IAmAScopeProvider>();

            // Act
            TrackedQuery.Result result = null;
            await Should.NotThrowAsync(async () => result = await queryProcessor.ExecuteAsync(new TrackedQuery()));

            // Assert — the Scoped dependency is disposed after the query, so the scope was owned; the provider was never asked
            result.HandlerDependency.IsDisposed.ShouldBeTrue();
            scopeProvider.CallCount.ShouldBe(0);
        }

        private static ServiceProvider Build()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry => registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandler>())
                .AddAsyncHandlers(registry => registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandlerAsync>());
            services.AddDarkerScopeProvider<ThrowingScopeProvider>(ScopeAffinity.AlwaysNew);

            return services.BuildServiceProvider();
        }
    }
}
