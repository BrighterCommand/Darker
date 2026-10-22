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
    public class SingletonHandlerLifetimeDecoratorReuseTests
    {
        [Fact]
        public void When_executing_two_queries_should_use_the_same_decorator_instance()
        {
            // Arrange — a Singleton handler lifetime keeps decorators Singleton (a known limit, out of scope for spec 015)
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(registry => registry.Register<FirstSharedResultQuery, SharedRecordingResult, FirstSharedResultQueryHandler>())
                .RegisterDecorator(typeof(InstanceRecordingDecorator<,>));

            using var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act
            var first = queryProcessor.Execute(new FirstSharedResultQuery());
            var second = queryProcessor.Execute(new FirstSharedResultQuery());

            // Assert
            first.DecoratorIds.Single().ShouldBe(second.DecoratorIds.Single());
        }
    }
}
