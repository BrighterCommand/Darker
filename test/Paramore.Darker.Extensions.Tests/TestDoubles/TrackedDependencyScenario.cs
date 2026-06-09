using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
            => BuildCore(options => options.HandlerLifetime = handlerLifetime, dependencyLifetime, validateScopes);

        /// <summary>
        /// Builds the same scenario but leaves <c>HandlerLifetime</c> unset, so it exercises the
        /// <em>default configuration path</em> (default <see cref="ServiceLifetime.Transient"/>)
        /// rather than an explicit setting — used by the AC9/NFR2 default-path guard.
        /// </summary>
        public static ServiceProvider BuildWithDefaultLifetime(
            ServiceLifetime dependencyLifetime,
            bool validateScopes = false)
            => BuildCore(configure: null, dependencyLifetime, validateScopes);

        private static ServiceProvider BuildCore(
            Action<DarkerOptions> configure,
            ServiceLifetime dependencyLifetime,
            bool validateScopes)
        {
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<DependencyTracker>();
            services.Add(new ServiceDescriptor(typeof(ITrackedDependency), typeof(TrackedDependency), dependencyLifetime));

            var builder = services
                .AddDarker(configure)
                .AddHandlers(registry =>
                {
                    registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandler>();
                    registry.Register<DecoratedTrackedQuery, DecoratedTrackedQuery.Result, DecoratedTrackedQueryHandler>();
                    registry.Register<ConcurrentTrackedQuery, ConcurrentTrackedQuery.Result, ConcurrentTrackedQueryHandler>();
                    registry.Register<ThrowingTrackedQuery, ThrowingTrackedQuery.Result, ThrowingTrackedQueryHandler>();
                })
                .AddAsyncHandlers(registry =>
                {
                    registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandlerAsync>();
                    registry.Register<DecoratedTrackedQuery, DecoratedTrackedQuery.Result, DecoratedTrackedQueryHandlerAsync>();
                    registry.Register<ConcurrentTrackedQuery, ConcurrentTrackedQuery.Result, ConcurrentTrackedQueryHandlerAsync>();
                    registry.Register<ThrowingTrackedQuery, ThrowingTrackedQuery.Result, ThrowingTrackedQueryHandlerAsync>();
                    registry.Register<CancellingTrackedQuery, CancellingTrackedQuery.Result, CancellingTrackedQueryHandlerAsync>();
                });

            builder.RegisterDecorator(typeof(TrackedDecorator<,>));
            builder.RegisterDecorator(typeof(TrackedDecoratorAsync<,>));

            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = validateScopes });
        }
    }
}
