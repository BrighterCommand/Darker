using System;
using LightInject;

namespace Paramore.Darker.LightInject
{
    internal sealed class LightInjectHandlerRegistry : QueryHandlerRegistry, IQueryHandlerDecoratorRegistry
    {
        private readonly ServiceContainer _container;

        public LightInjectHandlerRegistry(ServiceContainer container)
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