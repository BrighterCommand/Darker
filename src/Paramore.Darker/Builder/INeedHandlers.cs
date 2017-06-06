using System;

namespace Paramore.Darker.Builder
{
    public interface INeedHandlers
    {
        INeedRemoteQueries Handlers(IHandlerConfiguration handlerConfiguration);
        INeedRemoteQueries Handlers(IQueryHandlerRegistry handlerRegistry, IQueryHandlerFactory handlerFactory, IQueryHandlerDecoratorFactory decoratorFactory);
        INeedRemoteQueries Handlers(IQueryHandlerRegistry handlerRegistry, Func<Type, IQueryHandler> handlerFactory, Func<Type, IQueryHandlerDecorator> decoratorFactory);
    }
}