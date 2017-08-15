using System;

namespace Paramore.Darker.Builder
{
    public interface INeedHandlers
    {
        INeedAQueryContext Handlers(IHandlerConfiguration handlerConfiguration);
        INeedAQueryContext Handlers(IQueryHandlerRegistry handlerRegistry, IQueryHandlerFactory handlerFactory, IQueryHandlerDecoratorRegistry decoratorRegistry, IQueryHandlerDecoratorFactory decoratorFactory);
        INeedAQueryContext Handlers(IQueryHandlerRegistry handlerRegistry, Func<Type, IQueryHandler> handlerFactory, Action<Type> decoratorRegistry, Func<Type, IQueryHandlerDecorator> decoratorFactory);
    }
}