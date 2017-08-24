using System;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.AspNetCore
{
    internal sealed class AspNetHandlerFactory : IQueryHandlerFactory, IQueryHandlerDecoratorFactory
    {
        private readonly Lazy<IServiceProvider> _serviceProvider;

        public AspNetHandlerFactory(IServiceCollection services)
        {
            _serviceProvider = new Lazy<IServiceProvider>(services.BuildServiceProvider);
        }

        IQueryHandler IQueryHandlerFactory.Create(Type handlerType)
        {
            return (IQueryHandler)_serviceProvider.Value.GetService(handlerType);
        }

        void IQueryHandlerFactory.Release(IQueryHandler handler)
        {
            // no op
        }

        T IQueryHandlerDecoratorFactory.Create<T>(Type decoratorType)
        {
            return (T)_serviceProvider.Value.GetService(decoratorType);
        }

        void IQueryHandlerDecoratorFactory.Release<T>(T handler)
        {
            // no op
        }
    }
}