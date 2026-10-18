using System.Threading.Tasks;
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
    public class When_ambient_scope_adopted_on_async_paths_should_never_dispose_it
    {
        [Fact]
        public async Task When_executing_an_async_query_should_resolve_the_ambient_instance_and_leave_the_ambient_scope_alone()
        {
            // Arrange
            using var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var ambient = new RecordingAmbientScope(provider.CreateScope());
            var ambientDependency = ambient.Services.GetRequiredService<ITrackedDependency>();
            using var _ = AsyncLocalScopeProvider.Use(ambient);

            // Act
            var result = await queryProcessor.ExecuteAsync(new TrackedQuery(), cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            ReferenceEquals(result.HandlerDependency, ambientDependency).ShouldBeTrue();
            AssertAmbientScopeLeftAlone(ambient, ambientDependency);
        }

        [Fact]
        public async Task When_executing_a_stream_query_should_resolve_the_ambient_instance_and_leave_the_ambient_scope_alone()
        {
            // Arrange
            using var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var ambient = new RecordingAmbientScope(provider.CreateScope());
            var ambientDependency = ambient.Services.GetRequiredService<ITrackedDependency>();
            using var _ = AsyncLocalScopeProvider.Use(ambient);
            var query = new TrackedStreamQuery();
            query.Release();

            // Act
            await foreach (var __ in queryProcessor.ExecuteStream(query, cancellationToken: TestContext.Current.CancellationToken)) { }

            // Assert
            ReferenceEquals(query.HandlerDependency, ambientDependency).ShouldBeTrue();
            AssertAmbientScopeLeftAlone(ambient, ambientDependency);
        }

        private static void AssertAmbientScopeLeftAlone(RecordingAmbientScope ambient, ITrackedDependency ambientDependency)
        {
            ambientDependency.IsDisposed.ShouldBeFalse();
            ReferenceEquals(ambient.Services.GetRequiredService<ITrackedDependency>(), ambientDependency).ShouldBeTrue();
            ambient.DisposeCount.ShouldBe(0);
            ambient.DisposeAsyncCount.ShouldBe(0);
        }

        private static ServiceProvider Build()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddAsyncHandlers(registry => registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandlerAsync>())
                .AddStreamHandlers(registry => registry.Register<TrackedStreamQuery, string, TrackedStreamQueryHandler>())
                .RegisterDecorator(typeof(TrackedStreamDecorator<,>));
            services.AddDarkerScopeProvider<AsyncLocalScopeProvider>();

            return services.BuildServiceProvider();
        }
    }
}
