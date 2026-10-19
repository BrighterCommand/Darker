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
    public class AmbientScopeOfferedWithAlwaysNewTests
    {
        // (a) registered with an explicit AlwaysNew; (b) registered directly as the service, so only the
        // default affinity (AlwaysNew) applies
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void When_an_ambient_scope_is_offered_should_ignore_it_and_dispose_each_owned_scope(bool registeredWithExplicitAffinity)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry => registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandler>());

            if (registeredWithExplicitAffinity)
                services.AddDarkerScopeProvider<AsyncLocalScopeProvider>(ScopeAffinity.AlwaysNew);
            else
                services.AddSingleton<IAmAScopeProvider, AsyncLocalScopeProvider>();

            using var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var ambient = new RecordingAmbientScope(provider.CreateScope());
            var ambientDependency = ambient.Services.GetRequiredService<ITrackedDependency>();
            using var _ = AsyncLocalScopeProvider.Use(ambient);

            // Act
            var first = queryProcessor.Execute(new TrackedQuery());
            var second = queryProcessor.Execute(new TrackedQuery());

            // Assert — neither query used the ambient scope; each had a scope of its own and disposed it
            ReferenceEquals(first.HandlerDependency, ambientDependency).ShouldBeFalse();
            ReferenceEquals(second.HandlerDependency, ambientDependency).ShouldBeFalse();
            ReferenceEquals(first.HandlerDependency, second.HandlerDependency).ShouldBeFalse();
            first.HandlerDependency.IsDisposed.ShouldBeTrue();
            second.HandlerDependency.IsDisposed.ShouldBeTrue();
            ambientDependency.IsDisposed.ShouldBeFalse();
        }
    }
}
