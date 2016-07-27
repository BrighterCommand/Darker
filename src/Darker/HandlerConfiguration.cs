using System;

namespace Darker
{
    public sealed class HandlerConfiguration : IHandlerConfiguration
    {
        public IQueryHandlerRegistry HandlerRegistry { get; }
        public IQueryHandlerFactory HandlerFactory { get; }
        public IQueryHandlerDecoratorFactory DecoratorFactory { get; }

        public HandlerConfiguration(IQueryHandlerRegistry handlerRegistry, IQueryHandlerFactory handlerFactory, IQueryHandlerDecoratorFactory decoratorFactory)
        {
            if (handlerRegistry == null)
                throw new ArgumentNullException(nameof(handlerRegistry));
            if (handlerFactory == null)
                throw new ArgumentNullException(nameof(handlerFactory));
            if (decoratorFactory == null)
                throw new ArgumentNullException(nameof(decoratorFactory));

            HandlerRegistry = handlerRegistry;
            HandlerFactory = handlerFactory;
            DecoratorFactory = decoratorFactory;
        }
    }
}