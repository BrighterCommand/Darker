using System;

namespace Darker.Builder
{
    public interface INeedHandlers
    {
        INeedAQueryContext Handlers(IHandlerConfiguration handlerConfiguration);
        INeedAQueryContext Handlers(IQueryHandlerRegistry handlerRegistry, IQueryHandlerFactory handlerFactory, IQueryHandlerDecoratorFactory decoratorFactory);
        INeedAQueryContext Handlers(IQueryHandlerRegistry handlerRegistry, Func<Type, object> handlerFactory, Func<Type, object> decoratorFactory);
    }
}