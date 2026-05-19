using System;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    internal sealed class ServiceProviderHandlerFactory : IQueryHandlerFactory, IQueryHandlerFactoryAsync
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceProviderHandlerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        IQueryHandler IQueryHandlerFactory.Create(Type handlerType)
        {
            return (IQueryHandler) _serviceProvider.GetService(handlerType);
        }

        void IQueryHandlerFactory.Release(IQueryHandler handler)
        {
            var disposal = handler as IDisposable;
            disposal?.Dispose();
        }

        IQueryHandler IQueryHandlerFactoryAsync.Create(Type handlerType)
        {
            return (IQueryHandler) _serviceProvider.GetService(handlerType);
        }

        void IQueryHandlerFactoryAsync.Release(IQueryHandler handler)
        {
            var disposal = handler as IDisposable;
            disposal?.Dispose();
        }
    }
}