using System;

namespace Paramore.Darker.AspNetCore
{
    internal sealed class AspNetHandlerFactory : IQueryHandlerFactory, IQueryHandlerDecoratorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AspNetHandlerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        IQueryHandler IQueryHandlerFactory.Create(Type handlerType)
        {
            return (IQueryHandler)_serviceProvider.GetService(handlerType);
        }

        void IQueryHandlerFactory.Release(IQueryHandler handler)
        {
            var disposal = handler as IDisposable;
            disposal?.Dispose();
        }

        T IQueryHandlerDecoratorFactory.Create<T>(Type decoratorType)
        {
            return (T)_serviceProvider.GetService(decoratorType);
        }

        void IQueryHandlerDecoratorFactory.Release<T>(T handler)
        {
            var disposal = handler as IDisposable;
            disposal?.Dispose();
        }
    }
}