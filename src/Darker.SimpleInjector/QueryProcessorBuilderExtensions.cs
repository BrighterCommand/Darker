using System;
using Darker.Builder;
using SimpleInjector;

namespace Darker.SimpleInjector
{
    public static class QueryProcessorBuilderExtensions
    {
        public static INeedARequestContext SimpleInjectorHandlers(this INeedHandlers handlerBuilder, Container container, Action<HandlerSettings> settings)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var factory = new SimpleInjectorHandlerFactory(container);
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

            public HandlerConfiguration(IQueryHandlerRegistry handlerRegistry, SimpleInjectorHandlerFactory factory)
            {
                HandlerRegistry = handlerRegistry;
                HandlerFactory = factory;
                DecoratorFactory = factory;
            }
        }

        private sealed class SimpleInjectorHandlerFactory : IQueryHandlerFactory, IQueryHandlerDecoratorFactory
        {
            private readonly Container _container;

            public SimpleInjectorHandlerFactory(Container container)
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
