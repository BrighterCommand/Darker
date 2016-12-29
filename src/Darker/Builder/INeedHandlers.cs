using System;

namespace Darker.Builder
{
    public interface INeedHandlers
    {
        INeedARequestContext Handlers(IHandlerConfiguration handlerConfiguration);
        INeedARequestContext Handlers(IQueryHandlerRegistry handlerRegistry, IQueryHandlerFactory handlerFactory, IQueryHandlerDecoratorFactory decoratorFactory);
        INeedARequestContext Handlers(IQueryHandlerRegistry handlerRegistry, Func<Type, object> handlerFactory, Func<Type, object> decoratorFactory);
    }
}