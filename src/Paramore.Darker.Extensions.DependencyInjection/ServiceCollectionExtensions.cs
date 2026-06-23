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

            services.TryAdd(new ServiceDescriptor(typeof(IQueryProcessor), provider => BuildQueryProcessor(handlerRegistry, handlerRegistryAsync, provider, decoratorRegistry, options), options.QueryProcessorLifetime));

            return new ServiceCollectionDarkerHandlerBuilder(handlerRegistry, handlerRegistryAsync, decoratorRegistry, services);
        }

        private static QueryProcessor BuildQueryProcessor(
            IQueryHandlerRegistry handlerRegistry,
            IQueryHandlerRegistryAsync handlerRegistryAsync,
            IServiceProvider provider,
            ServiceCollectionDecoratorRegistry decoratorRegistry,
            DarkerOptions options)
        {
            var loggerFactory = provider.GetService<ILoggerFactory>();
            ApplicationLogging.LoggerFactory = loggerFactory;

            var componentFactory = new ServiceProviderComponentFactory(provider, options.HandlerLifetime);
            var policyRegistry = provider.GetService<Polly.Registry.IPolicyRegistry<string>>();
            var resiliencePipelineProvider = provider.GetService<Polly.Registry.ResiliencePipelineProvider<string>>();

            return new QueryProcessor(
                new HandlerConfiguration(
                    handlerRegistry, componentFactory, decoratorRegistry, componentFactory,
                    handlerRegistryAsync, componentFactory, decoratorRegistry, componentFactory),
                options.QueryContextFactory, policyRegistry, resiliencePipelineProvider);
        }
    }
}