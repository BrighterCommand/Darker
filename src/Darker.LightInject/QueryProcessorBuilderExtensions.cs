using System;
using Darker.Builder;
using LightInject;

namespace Darker.LightInject
{
    public static class QueryProcessorBuilderExtensions
    {
        public static INeedPolicies LightInjectHandlers(this INeedHandlers handlerBuilder, ServiceContainer container, Action<HandlerSettings> settings)
        {
            var factory = new HandlerFactory(container);
            var handlerRegistry = new QueryHandlerRegistry();

            var handlerSettings = new HandlerSettings(container, handlerRegistry);
            settings(handlerSettings);

            var handlerConfiguration = new HandlerConfiguration(handlerRegistry, factory);
            return handlerBuilder.Handlers(handlerConfiguration);
        }

        private sealed class HandlerConfiguration : IHandlerConfiguration
        {
            public IQueryHandlerRegistry HandlerRegistry { get; }
            public IQueryHandlerFactory HandlerFactory { get; }
            public IQueryHandlerDecoratorFactory DecoratorFactory { get; }

            public HandlerConfiguration(IQueryHandlerRegistry handlerRegistry, HandlerFactory factory)
            {
                HandlerRegistry = handlerRegistry;
                HandlerFactory = factory;
                DecoratorFactory = factory;
            }
        }

        private sealed class HandlerFactory : IQueryHandlerFactory, IQueryHandlerDecoratorFactory
        {
            private readonly ServiceContainer _container;

            public HandlerFactory(ServiceContainer container)
            {
                _container = container;
            }

            T IQueryHandlerFactory.Create<T>(Type handlerType)
            {
                return (T)_container.GetInstance(handlerType);
            }

            T IQueryHandlerDecoratorFactory.Create<T>(Type decoratorType)
            {
                return (T)_container.GetInstance(decoratorType);
            }
        }
    }
}
