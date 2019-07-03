using System;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IDarkerHandlerBuilder AddDarker(this IServiceCollection services, Action<DarkerOptions> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var options = new DarkerOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            var registry = new ServiceCollectionHandlerRegistry(services, options.HandlerLifetime);
            services.AddSingleton(registry);

            services.Add(new ServiceDescriptor(typeof(IQueryProcessor), BuildDarker, options.HandlerLifetime));

            return new ServiceCollectionDarkerHandlerBuilder(services, registry);
        }

        private static IQueryProcessor BuildDarker(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetService<DarkerOptions>();
            var factory = new ServiceProviderHandlerFactory(serviceProvider);
            var registry = serviceProvider.GetService<ServiceCollectionHandlerRegistry>();

            var builder = QueryProcessorBuilder.With()
                .Handlers(registry, factory, registry, factory)
                .QueryContextFactory(options.QueryContextFactory);


            return builder.Build();
        }
    }
}