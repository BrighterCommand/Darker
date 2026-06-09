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

        IQueryHandler IQueryHandlerFactory.Create(Type handlerType, IAmALifetime lifetime)
        {
            return (IQueryHandler) _serviceProvider.GetService(handlerType);
        }

        void IQueryHandlerFactory.Release(IQueryHandler handler, IAmALifetime lifetime)
        {
            var disposal = handler as IDisposable;
            disposal?.Dispose();
        }

        IQueryHandler IQueryHandlerFactoryAsync.Create(Type handlerType, IAmALifetime lifetime)
        {
            return (IQueryHandler) _serviceProvider.GetService(handlerType);
        }

        void IQueryHandlerFactoryAsync.Release(IQueryHandler handler, IAmALifetime lifetime)
        {
            var disposal = handler as IDisposable;
            disposal?.Dispose();
        }
    }
}