using System;
using LightInject;

namespace Paramore.Darker.LightInject
{
    internal sealed class LightInjectHandlerFactory : IQueryHandlerFactory, IQueryHandlerDecoratorFactory
    {
        private readonly ServiceContainer _container;

        public LightInjectHandlerFactory(ServiceContainer container)
        {
            _container = container;
        }

        IQueryHandler IQueryHandlerFactory.Create(Type handlerType)
        {
            return (IQueryHandler)_container.GetInstance(handlerType);
        }

        void IQueryHandlerFactory.Release(IQueryHandler handler)
        {
            // no op
        }

        T IQueryHandlerDecoratorFactory.Create<T>(Type decoratorType)
        {
            return (T)_container.GetInstance(decoratorType);
        }

        void IQueryHandlerDecoratorFactory.Release<T>(T handler)
        {
            // no op
        }
    }
}