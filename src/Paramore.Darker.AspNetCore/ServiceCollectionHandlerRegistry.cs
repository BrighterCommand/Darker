using System;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.AspNetCore
{
    internal sealed class ServiceCollectionHandlerRegistry : QueryHandlerRegistry
    {
        private readonly ServiceLifetime _lifetime;
        private readonly IServiceCollection _services;

        public ServiceCollectionHandlerRegistry(IServiceCollection services, ServiceLifetime lifetime)
        {
            _services = services;
            _lifetime = lifetime;
        }

        public override void Register(Type queryType, Type resultType, Type handlerType)
        {
            _services.Add(new ServiceDescriptor(handlerType, handlerType, _lifetime));

            base.Register(queryType, resultType, handlerType);
        }
    }
}