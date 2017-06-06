using System;
using LightInject;
using Paramore.Darker.Builder;

namespace Paramore.Darker.LightInject
{
    public static class QueryProcessorBuilderExtensions
    {
        public static INeedRemoteQueries LightInjectHandlers(this INeedHandlers handlerBuilder, ServiceContainer container, Action<HandlerSettings> settings)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var factory = new LightInjectHandlerFactory(container);
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

            public HandlerConfiguration(IQueryHandlerRegistry handlerRegistry, LightInjectHandlerFactory factory)
            {
                HandlerRegistry = handlerRegistry;
                HandlerFactory = factory;
                DecoratorFactory = factory;
            }
        }

        private sealed class LightInjectHandlerFactory : IQueryHandlerFactory, IQueryHandlerDecoratorFactory
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
                // no nop
            }

            T IQueryHandlerDecoratorFactory.Create<T>(Type decoratorType)
            {
                return (T)_container.GetInstance(decoratorType);
            }

            void IQueryHandlerDecoratorFactory.Release<T>(T handler)
            {
                // no nop
            }
        }
    }
}
