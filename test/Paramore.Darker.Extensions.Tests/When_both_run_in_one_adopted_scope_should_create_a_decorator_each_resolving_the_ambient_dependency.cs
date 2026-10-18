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
    public class When_two_query_types_share_a_result_type_in_one_ambient_scope_should_get_distinct_decorators
    {
        [Fact]
        public void When_both_run_in_one_adopted_scope_should_create_a_decorator_each_resolving_the_ambient_dependency()
        {
            // Arrange — Scoped handlers and one ambient scope; both queries return SharedRecordingResult,
            // and decorators are closed over IQuery<TResult>, so they would share a Scoped decorator
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry =>
                {
                    registry.Register<FirstSharedResultQuery, SharedRecordingResult, FirstSharedResultQueryHandler>();
                    registry.Register<SecondSharedResultQuery, SharedRecordingResult, SecondSharedResultQueryHandler>();
                })
                .RegisterDecorator(typeof(InstanceRecordingDecorator<,>));
            services.AddDarkerScopeProvider<AsyncLocalScopeProvider>();

            using var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var ambient = new RecordingAmbientScope(provider.CreateScope());
            var ambientDependency = ambient.Services.GetRequiredService<ITrackedDependency>();
            using var _ = AsyncLocalScopeProvider.Use(ambient);

            // Act
            var first = queryProcessor.Execute(new FirstSharedResultQuery());
            var second = queryProcessor.Execute(new SecondSharedResultQuery());

            // Assert — a decorator instance for each query
            first.DecoratorIds.Single().ShouldNotBe(second.DecoratorIds.Single());

            // Assert — each decorator still resolved the ambient scope's dependency
            ReferenceEquals(first.DecoratorDependencies.Single(), ambientDependency).ShouldBeTrue();
            ReferenceEquals(second.DecoratorDependencies.Single(), ambientDependency).ShouldBeTrue();
        }
    }
}
