using System;
using SimpleInjector;

namespace Paramore.Darker.SimpleInjector
{
    internal sealed class SimpleInjectorHandlerFactory : IQueryHandlerFactory, IQueryHandlerDecoratorFactory
    {
        private readonly Container _container;

        public SimpleInjectorHandlerFactory(Container container)
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