using System;

namespace Darker.Builder
{
    public interface INeedHandlers
    {
        INeedPolicies Handlers(IHandlerConfiguration handlerConfiguration);
        INeedPolicies Handlers(IQueryHandlerRegistry handlerRegistry, IQueryHandlerFactory handlerFactory, IQueryHandlerDecoratorFactory decoratorFactory);
        INeedPolicies Handlers(IQueryHandlerRegistry handlerRegistry, Func<Type, object> handlerFactory, Func<Type, object> decoratorFactory);
    }
}