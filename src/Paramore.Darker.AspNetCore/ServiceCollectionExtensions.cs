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

            var handlerRegistry = new ServiceCollectionHandlerRegistry(services, options.HandlerLifetime);

            var decoratorRegistry = new ServiceCollectionDecoratorRegistry(services, options.HandlerLifetime);
            decoratorRegistry.RegisterDefaultDecorators();

            var contextBag = new DarkerContextBag();
            
            services.Add(new ServiceDescriptor(typeof(IQueryProcessor), provider =>  new QueryProcessor(
                new HandlerConfiguration(handlerRegistry, new ServiceProviderHandlerFactory(provider), decoratorRegistry,
                    new ServiceProviderHandlerDecoratorFactory(provider)), options.QueryContextFactory, contextBag), options.QueryProcessorLifetime));

            return new ServiceCollectionDarkerHandlerBuilder(handlerRegistry, decoratorRegistry, contextBag);
        }
    }
}