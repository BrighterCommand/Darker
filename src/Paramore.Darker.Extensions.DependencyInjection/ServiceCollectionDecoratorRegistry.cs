using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Paramore.Darker.Decorators;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    public class ServiceCollectionDecoratorRegistry : IQueryHandlerDecoratorRegistry, IQueryHandlerDecoratorRegistryAsync
    {
        private readonly ServiceLifetime _optionsHandlerLifetime;
        private readonly IServiceCollection _services;

        public ServiceCollectionDecoratorRegistry(IServiceCollection services, ServiceLifetime optionsHandlerLifetime)
        {
            _services = services;
            _optionsHandlerLifetime = optionsHandlerLifetime;
        }

        public void Register(Type decoratorType)
        {
            _services.TryAdd(new ServiceDescriptor(decoratorType, decoratorType, _optionsHandlerLifetime));
        }
        
        public void RegisterDefaultDecorators()
        {
            Register(typeof(FallbackPolicyDecorator<,>));
            Register(typeof(FallbackPolicyDecoratorAsync<,>));
        }
    }
}