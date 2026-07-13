using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Logging;
using Paramore.Darker.Observability;

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
            var handlerRegistryStream = new ServiceCollectionStreamHandlerRegistry(services, options.HandlerLifetime);

            var decoratorRegistry = new ServiceCollectionDecoratorRegistry(services, options.HandlerLifetime);
            decoratorRegistry.RegisterDefaultDecorators();

            services.TryAdd(new ServiceDescriptor(typeof(IQueryProcessor), provider => BuildQueryProcessor(handlerRegistry, handlerRegistryAsync, handlerRegistryStream, provider, decoratorRegistry, options), options.QueryProcessorLifetime));

            return new ServiceCollectionDarkerHandlerBuilder(handlerRegistry, handlerRegistryAsync, handlerRegistryStream, decoratorRegistry, services);
        }

        private static QueryProcessor BuildQueryProcessor(
            IQueryHandlerRegistry handlerRegistry,
            IQueryHandlerRegistryAsync handlerRegistryAsync,
            IStreamQueryHandlerRegistry handlerRegistryStream,
            IServiceProvider provider,
            ServiceCollectionDecoratorRegistry decoratorRegistry,
            DarkerOptions options)
        {
            var loggerFactory = provider.GetService<ILoggerFactory>();
            ApplicationLogging.LoggerFactory = loggerFactory;

            var componentFactory = new ServiceProviderComponentFactory(provider, options.HandlerLifetime);
            var policyRegistry = provider.GetService<Polly.Registry.IPolicyRegistry<string>>();
            var resiliencePipelineProvider = provider.GetService<Polly.Registry.ResiliencePipelineProvider<string>>();
            var tracer = provider.GetService<IAmADarkerTracer>();

            return new QueryProcessor(
                new HandlerConfiguration(
                    handlerRegistry, componentFactory, decoratorRegistry, componentFactory,
                    handlerRegistryAsync, componentFactory, decoratorRegistry, componentFactory,
                    handlerRegistryStream),
                options.QueryContextFactory, policyRegistry, resiliencePipelineProvider,
                tracer, options.InstrumentationOptions);
        }
    }
}