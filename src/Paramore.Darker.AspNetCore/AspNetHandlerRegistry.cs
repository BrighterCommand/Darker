using System;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.AspNetCore
{
    internal sealed class AspNetHandlerRegistry : QueryHandlerRegistry, IQueryHandlerDecoratorRegistry
    {
        private readonly IServiceCollection _services;
        private readonly ServiceLifetime _lifetime;

        public AspNetHandlerRegistry(IServiceCollection services, ServiceLifetime lifetime)
        {
            _services = services;
            _lifetime = lifetime;
        }

        public override void Register(Type queryType, Type resultType, Type handlerType)
        {
            _services.Add(new ServiceDescriptor(handlerType, handlerType, _lifetime));

            base.Register(queryType, resultType, handlerType);
        }

        public void Register(Type decoratorType)
        {
            _services.Add(new ServiceDescriptor(decoratorType, decoratorType, _lifetime));
        }
    }
}