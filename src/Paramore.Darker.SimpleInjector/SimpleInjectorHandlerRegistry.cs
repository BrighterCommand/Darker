using System;
using SimpleInjector;

namespace Paramore.Darker.SimpleInjector
{
    internal sealed class SimpleInjectorHandlerRegistry : QueryHandlerRegistry, IQueryHandlerDecoratorRegistry
    {
        private readonly Container _container;

        public SimpleInjectorHandlerRegistry(Container container)
        {
            _container = container;
        }

        public override void Register(Type queryType, Type resultType, Type handlerType)
        {
            _container.Register(handlerType);
            base.Register(queryType, resultType, handlerType);
        }

        public void Register(Type decoratorType)
        {
            _container.Register(decoratorType);
        }
    }
}