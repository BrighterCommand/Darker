using System;

namespace Paramore.Darker.AspNetCore
{
    internal sealed class ServiceProviderHandlerDecoratorFactory : IQueryHandlerDecoratorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceProviderHandlerDecoratorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Create<T>(Type decoratorType) where T : IQueryHandlerDecorator
        {
            return (T) _serviceProvider.GetService(decoratorType);
        }

        public void Release<T>(T handler) where T : IQueryHandlerDecorator
        {
            var disposal = handler as IDisposable;
            disposal?.Dispose();
        }
    }
}