using System;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.AspNetCore
{
    internal sealed class AspNetHandlerFactory : IQueryHandlerFactory, IQueryHandlerDecoratorFactory
    {
        private readonly IServiceCollection _services;

        public AspNetHandlerFactory(IServiceCollection services)
        {
            _services = services;
        }

        IQueryHandler IQueryHandlerFactory.Create(Type handlerType)
        {
            return (IQueryHandler)_services.BuildServiceProvider().GetService(handlerType);
        }

        void IQueryHandlerFactory.Release(IQueryHandler handler)
        {
            // no op
        }

        T IQueryHandlerDecoratorFactory.Create<T>(Type decoratorType)
        {
            return (T)_services.BuildServiceProvider().GetService(decoratorType);
        }

        void IQueryHandlerDecoratorFactory.Release<T>(T handler)
        {
            // no op
        }
    }
}