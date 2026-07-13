using System;

namespace Paramore.Darker
{
    public sealed class HandlerConfiguration : IHandlerConfiguration
    {
        public IQueryHandlerRegistry HandlerRegistry { get; }
        public IQueryHandlerFactory HandlerFactory { get; }
        public IQueryHandlerDecoratorRegistry DecoratorRegistry { get; }
        public IQueryHandlerDecoratorFactory DecoratorFactory { get; }

        public IQueryHandlerRegistryAsync HandlerRegistryAsync { get; }
        public IQueryHandlerFactoryAsync HandlerFactoryAsync { get; }
        public IQueryHandlerDecoratorRegistryAsync DecoratorRegistryAsync { get; }
        public IQueryHandlerDecoratorFactoryAsync DecoratorFactoryAsync { get; }

        /// <inheritdoc/>
        public IStreamQueryHandlerRegistry StreamHandlerRegistry { get; }

        public HandlerConfiguration(
            IQueryHandlerRegistry handlerRegistry,
            IQueryHandlerFactory handlerFactory,
            IQueryHandlerDecoratorRegistry decoratorRegistry,
            IQueryHandlerDecoratorFactory decoratorFactory)
            : this(handlerRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                   null, null, null, null)
        {
        }

        public HandlerConfiguration(
            IQueryHandlerRegistry handlerRegistry,
            IQueryHandlerFactory handlerFactory,
            IQueryHandlerDecoratorRegistry decoratorRegistry,
            IQueryHandlerDecoratorFactory decoratorFactory,
            IQueryHandlerRegistryAsync handlerRegistryAsync,
            IQueryHandlerFactoryAsync handlerFactoryAsync,
            IQueryHandlerDecoratorRegistryAsync decoratorRegistryAsync,
            IQueryHandlerDecoratorFactoryAsync decoratorFactoryAsync)
        {
            HandlerRegistry = handlerRegistry ?? throw new ArgumentNullException(nameof(handlerRegistry));
            HandlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
            DecoratorRegistry = decoratorRegistry ?? throw new ArgumentNullException(nameof(decoratorRegistry));
            DecoratorFactory = decoratorFactory ?? throw new ArgumentNullException(nameof(decoratorFactory));

            HandlerRegistryAsync = handlerRegistryAsync;
            HandlerFactoryAsync = handlerFactoryAsync;
            DecoratorRegistryAsync = decoratorRegistryAsync;
            DecoratorFactoryAsync = decoratorFactoryAsync;
        }

        public HandlerConfiguration(
            IQueryHandlerRegistry handlerRegistry,
            IQueryHandlerFactory handlerFactory,
            IQueryHandlerDecoratorRegistry decoratorRegistry,
            IQueryHandlerDecoratorFactory decoratorFactory,
            IQueryHandlerRegistryAsync handlerRegistryAsync,
            IQueryHandlerFactoryAsync handlerFactoryAsync,
            IQueryHandlerDecoratorRegistryAsync decoratorRegistryAsync,
            IQueryHandlerDecoratorFactoryAsync decoratorFactoryAsync,
            IStreamQueryHandlerRegistry streamHandlerRegistry)
            : this(handlerRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                   handlerRegistryAsync, handlerFactoryAsync, decoratorRegistryAsync, decoratorFactoryAsync)
        {
            StreamHandlerRegistry = streamHandlerRegistry;
        }
    }
}
