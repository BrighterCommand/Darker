using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Logging;

namespace Paramore.Darker.Extensions.DependencyInjection
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

            var handlerRegistry = new ServiceCollectionHandlerRegistry(services, options.HandlerLifetime);
            var handlerRegistryAsync = new ServiceCollectionHandlerRegistryAsync(services, options.HandlerLifetime);

            var decoratorRegistry = new ServiceCollectionDecoratorRegistry(services, options.HandlerLifetime);
            decoratorRegistry.RegisterDefaultDecorators();

            var contextBag = new DarkerContextBag();

            services.TryAdd(new ServiceDescriptor(typeof(IQueryProcessor), provider => BuildQueryProcessor(handlerRegistry, handlerRegistryAsync, provider, decoratorRegistry, options, contextBag), options.QueryProcessorLifetime));


            return new ServiceCollectionDarkerHandlerBuilder(handlerRegistry, handlerRegistryAsync, decoratorRegistry, contextBag);
        }

        private static QueryProcessor BuildQueryProcessor(
            IQueryHandlerRegistry handlerRegistry,
            IQueryHandlerRegistryAsync handlerRegistryAsync,
            IServiceProvider provider,
            ServiceCollectionDecoratorRegistry decoratorRegistry,
            DarkerOptions options,
            DarkerContextBag contextBag)
        {
            var loggerFactory = provider.GetService<ILoggerFactory>();
            ApplicationLogging.LoggerFactory = loggerFactory;

            var handlerFactory = new ServiceProviderHandlerFactory(provider);
            var decoratorFactory = new ServiceProviderHandlerDecoratorFactory(provider);

            return new QueryProcessor(
                new HandlerConfiguration(
                    handlerRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                    handlerRegistryAsync, handlerFactory, decoratorRegistry, decoratorFactory),
                options.QueryContextFactory, contextBag);
        }
    }
}