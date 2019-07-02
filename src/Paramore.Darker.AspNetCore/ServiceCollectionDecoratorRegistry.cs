using System;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.AspNetCore
{
    public class ServiceCollectionDecoratorRegistry : IQueryHandlerDecoratorRegistry
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
            _services.Add(new ServiceDescriptor(decoratorType, decoratorType, _optionsHandlerLifetime));
        }
    }
}