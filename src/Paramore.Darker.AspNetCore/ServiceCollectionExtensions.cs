using System;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Builder;
using Paramore.Darker.Exceptions;

namespace Paramore.Darker.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IDarkerHandlerBuilder AddDarker(this IServiceCollection services,
            Action<DarkerOptions> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var options = new DarkerOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);

            var handlerRegistry = new ServiceCollectionHandlerRegistry(services, options.HandlerLifetime);
            services.AddSingleton(handlerRegistry);

            var decoratorRegistry = new ServiceCollectionDecoratorRegistry(services, options.HandlerLifetime);
            services.AddSingleton(decoratorRegistry);

            var contextBag = new DarkerContextBag();
            services.AddSingleton(contextBag);

            services.Add(new ServiceDescriptor(typeof(IQueryProcessor), BuildDarker, options.QueryProcessorLifetime));

            return new ServiceCollectionDarkerHandlerBuilder(handlerRegistry, decoratorRegistry, contextBag);
        }

        private static IQueryProcessor BuildDarker(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetService<DarkerOptions>();
            var handlerFactory = new ServiceProviderHandlerFactory(serviceProvider);
            var handlerRegistry = serviceProvider.GetService<ServiceCollectionHandlerRegistry>();

            var decoratorFactory = new ServiceProviderHandlerDecoratorFactory(serviceProvider);
            var decoratorRegistry = serviceProvider.GetService<ServiceCollectionDecoratorRegistry>();

            var contextBag = serviceProvider.GetService<DarkerContextBag>();

            var builder = QueryProcessorBuilder.With()
                .Handlers(handlerRegistry, handlerFactory, decoratorRegistry, decoratorFactory)
                .QueryContextFactory(options.QueryContextFactory) as QueryProcessorBuilder;

            if (builder == null)
                throw new ConfigurationException("Could not build a QueryProcessorBuilder");

            builder.AddContextBag(contextBag);

            return builder.Build();
        }

        private static TBuilder AddContextBag<TBuilder>(this TBuilder builder, DarkerContextBag contextBag)
            where TBuilder : IQueryProcessorExtensionBuilder
        {
            foreach (var item in contextBag) builder.AddContextBagItem(item.Key, item.Value);

            return builder;
        }
    }
}