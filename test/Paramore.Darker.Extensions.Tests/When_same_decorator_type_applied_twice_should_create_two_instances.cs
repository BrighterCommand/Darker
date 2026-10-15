using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class When_same_decorator_type_applied_twice_should_create_two_instances
    {
        [Fact]
        public void When_the_handler_lifetime_is_scoped_should_give_each_application_its_own_decorator_sharing_the_scoped_dependency()
        {
            // Arrange — Scoped handlers, an owned scope (no provider), and a handler that applies one decorator type twice
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry => registry.Register<RepeatedDecoratorQuery, SharedRecordingResult, RepeatedDecoratorQueryHandler>())
                .RegisterDecorator(typeof(InstanceRecordingDecorator<,>));

            using var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act
            var result = queryProcessor.Execute(new RepeatedDecoratorQuery());

            // Assert — two different decorator instances, where one Scoped instance would have been initialised twice
            result.DecoratorIds.Count.ShouldBe(2);
            result.DecoratorIds[0].ShouldNotBe(result.DecoratorIds[1]);

            // Assert — both still share the handler's Scoped dependency
            ReferenceEquals(result.DecoratorDependencies[0], result.HandlerDependency).ShouldBeTrue();
            ReferenceEquals(result.DecoratorDependencies[1], result.HandlerDependency).ShouldBeTrue();
        }
    }
}
