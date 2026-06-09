using System;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    internal sealed class ServiceProviderHandlerDecoratorFactory : IQueryHandlerDecoratorFactory, IQueryHandlerDecoratorFactoryAsync
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceProviderHandlerDecoratorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Create<T>(Type decoratorType, IAmALifetime lifetime) where T : IQueryHandlerDecorator
        {
            return (T) _serviceProvider.GetService(decoratorType);
        }

        public void Release<T>(T handler, IAmALifetime lifetime) where T : IQueryHandlerDecorator
        {
            var disposal = handler as IDisposable;
            disposal?.Dispose();
        }
    }
}