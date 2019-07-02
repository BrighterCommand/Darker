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
            services.AddSingleton<DarkerOptions>(options);
            var registry = new AspNetHandlerRegistry(services, options.HandlerLifetime);
            services.AddSingleton<AspNetHandlerRegistry>(registry);

            services.Add(new ServiceDescriptor(typeof(IQueryProcessor), BuildDarker, options.HandlerLifetime));

            return new AspNetHandlerBuilder(services, registry);
        }

        private static IQueryProcessor  BuildDarker(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetService<DarkerOptions>();
            var factory = new AspNetHandlerFactory(serviceProvider);
            var registry = serviceProvider.GetService<AspNetHandlerRegistry>();

            var builder = QueryProcessorBuilder.With()
                .Handlers(registry, factory, registry, factory)
                .QueryContextFactory(options.QueryContextFactory);

            return builder.Build();
        }
    }
}