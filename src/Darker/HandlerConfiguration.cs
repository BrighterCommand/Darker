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
            HandlerRegistry = handlerRegistry ?? throw new ArgumentNullException(nameof(handlerRegistry));
            HandlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
            DecoratorFactory = decoratorFactory ?? throw new ArgumentNullException(nameof(decoratorFactory));
        }
    }
}