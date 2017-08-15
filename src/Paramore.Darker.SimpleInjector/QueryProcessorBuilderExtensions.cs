using System;
using Paramore.Darker.Builder;
using SimpleInjector;

namespace Paramore.Darker.SimpleInjector
{
    public static class QueryProcessorBuilderExtensions
    {
        public static INeedAQueryContext SimpleInjectorHandlers(this INeedHandlers handlerBuilder, Container container, Action<HandlerSettings> settings)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var factory = new HandlerFactory(container);
            var decoratorRegistry = new DecoratorRegistry(container);
            var handlerRegistry = new QueryHandlerRegistry();

            var handlerSettings = new HandlerSettings(container, handlerRegistry);
            settings(handlerSettings);

            var handlerConfiguration = new HandlerConfiguration(handlerRegistry, decoratorRegistry, factory);
            return handlerBuilder.Handlers(handlerConfiguration);
        }

        private sealed class HandlerConfiguration : IHandlerConfiguration
        {
            public IQueryHandlerRegistry HandlerRegistry { get; }
            public IQueryHandlerFactory HandlerFactory { get; }
            public IQueryHandlerDecoratorRegistry DecoratorRegistry { get; }
            public IQueryHandlerDecoratorFactory DecoratorFactory { get; }

            public HandlerConfiguration(IQueryHandlerRegistry handlerRegistry, IQueryHandlerDecoratorRegistry decoratorRegistry, HandlerFactory factory)
            {
                HandlerRegistry = handlerRegistry;
                HandlerFactory = factory;
                DecoratorRegistry = decoratorRegistry;
                DecoratorFactory = factory;
            }
        }
        
        private sealed class DecoratorRegistry : IQueryHandlerDecoratorRegistry
        {
            private readonly Container _container;

            public DecoratorRegistry(Container container)
            {
                _container = container;
            }
            
            public void Register(Type decoratorType)
            {
                _container.Register(decoratorType);
            }
        }

        private sealed class HandlerFactory : IQueryHandlerFactory, IQueryHandlerDecoratorFactory
        {
            private readonly Container _container;

            public HandlerFactory(Container container)
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
}
