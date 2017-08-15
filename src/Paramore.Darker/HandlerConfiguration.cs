using System;

namespace Paramore.Darker
{
    public sealed class HandlerConfiguration : IHandlerConfiguration
    {
        public IQueryHandlerRegistry HandlerRegistry { get; }
        public IQueryHandlerFactory HandlerFactory { get; }
        public IQueryHandlerDecoratorRegistry DecoratorRegistry { get; }
        public IQueryHandlerDecoratorFactory DecoratorFactory { get; }

        public HandlerConfiguration(
            IQueryHandlerRegistry handlerRegistry,
            IQueryHandlerFactory handlerFactory,
            IQueryHandlerDecoratorRegistry decoratorRegistry,
            IQueryHandlerDecoratorFactory decoratorFactory)
        {
            HandlerRegistry = handlerRegistry ?? throw new ArgumentNullException(nameof(handlerRegistry));
            HandlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
            DecoratorRegistry = decoratorRegistry ?? throw new ArgumentNullException(nameof(decoratorRegistry));
            DecoratorFactory = decoratorFactory ?? throw new ArgumentNullException(nameof(decoratorFactory));
        }
    }
}