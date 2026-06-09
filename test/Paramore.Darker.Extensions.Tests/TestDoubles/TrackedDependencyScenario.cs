using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// Builds a real Microsoft.Extensions.DependencyInjection container wired with Darker and the
    /// tracked test doubles, so the lifetime acceptance tests exercise the real DI factory
    /// (Real &gt; Simple &gt; InMemory &gt; Mock). The <see cref="DependencyTracker"/> is registered as a
    /// singleton and is resolvable from the returned provider for assertions.
    /// </summary>
    public static class TrackedDependencyScenario
    {
        public static ServiceProvider Build(
            ServiceLifetime handlerLifetime,
            ServiceLifetime dependencyLifetime,
            bool validateScopes = false)
        {
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton<DependencyTracker>();
            services.Add(new ServiceDescriptor(typeof(ITrackedDependency), typeof(TrackedDependency), dependencyLifetime));

            var builder = services
                .AddDarker(options => options.HandlerLifetime = handlerLifetime)
                .AddHandlers(registry =>
                {
                    registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandler>();
                    registry.Register<DecoratedTrackedQuery, DecoratedTrackedQuery.Result, DecoratedTrackedQueryHandler>();
                })
                .AddAsyncHandlers(registry =>
                {
                    registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandlerAsync>();
                    registry.Register<DecoratedTrackedQuery, DecoratedTrackedQuery.Result, DecoratedTrackedQueryHandlerAsync>();
                });

            builder.RegisterDecorator(typeof(TrackedDecorator<,>));
            builder.RegisterDecorator(typeof(TrackedDecoratorAsync<,>));

            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = validateScopes });
        }
    }
}
