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
    public class When_decorated_query_runs_should_resolve_decorator_from_handler_scope
    {
        [Fact]
        public void When_executing_with_an_adopted_scope_should_give_handler_and_decorator_the_ambient_instance()
        {
            // Arrange
            using var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var ambient = new RecordingAmbientScope(provider.CreateScope());
            var ambientDependency = ambient.Services.GetRequiredService<ITrackedDependency>();
            using var _ = AsyncLocalScopeProvider.Use(ambient);

            // Act
            var result = queryProcessor.Execute(new DecoratedTrackedQuery());

            // Assert
            ReferenceEquals(result.HandlerDependency, ambientDependency).ShouldBeTrue();
            ReferenceEquals(result.DecoratorDependency, ambientDependency).ShouldBeTrue();
        }

        [Fact]
        public async Task When_executing_async_with_an_adopted_scope_should_give_handler_and_decorator_the_ambient_instance()
        {
            // Arrange
            using var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var ambient = new RecordingAmbientScope(provider.CreateScope());
            var ambientDependency = ambient.Services.GetRequiredService<ITrackedDependency>();
            using var _ = AsyncLocalScopeProvider.Use(ambient);

            // Act
            var result = await queryProcessor.ExecuteAsync(new DecoratedTrackedQuery(), cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            ReferenceEquals(result.HandlerDependency, ambientDependency).ShouldBeTrue();
            ReferenceEquals(result.DecoratorDependency, ambientDependency).ShouldBeTrue();
        }

        [Fact]
        public async Task When_executing_a_stream_with_an_adopted_scope_should_give_handler_and_decorator_the_ambient_instance()
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
            ReferenceEquals(query.DecoratorDependency, ambientDependency).ShouldBeTrue();
        }

        private static ServiceProvider Build()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            var builder = services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry => registry.Register<DecoratedTrackedQuery, DecoratedTrackedQuery.Result, DecoratedTrackedQueryHandler>())
                .AddAsyncHandlers(registry => registry.Register<DecoratedTrackedQuery, DecoratedTrackedQuery.Result, DecoratedTrackedQueryHandlerAsync>())
                .AddStreamHandlers(registry => registry.Register<TrackedStreamQuery, string, TrackedStreamQueryHandler>());
            builder.RegisterDecorator(typeof(TrackedDecorator<,>));
            builder.RegisterDecorator(typeof(TrackedDecoratorAsync<,>));
            builder.RegisterDecorator(typeof(TrackedStreamDecorator<,>));
            services.AddDarkerScopeProvider<AsyncLocalScopeProvider>();

            return services.BuildServiceProvider();
        }
    }
}
