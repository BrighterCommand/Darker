using System;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.AspNetCore
{
    internal sealed class AspNetHandlerRegistry : QueryHandlerRegistry, IQueryHandlerDecoratorRegistry
    {
        private readonly IServiceCollection _services;

        public AspNetHandlerRegistry(IServiceCollection services)
        {
            _services = services;
        }

        public override void Register(Type queryType, Type resultType, Type handlerType)
        {
            _services.AddTransient(handlerType);
            base.Register(queryType, resultType, handlerType);
        }

        public void Register(Type decoratorType)
        {
            _services.AddTransient(decoratorType);
        }
    }
}