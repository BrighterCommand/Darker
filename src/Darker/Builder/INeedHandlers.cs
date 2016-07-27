namespace Darker.Builder
{
    public interface INeedHandlers
    {
        INeedPolicies Handlers(IHandlerConfiguration handlerConfiguration);
        INeedPolicies Handlers(IQueryHandlerRegistry handlerRegistry, IQueryHandlerFactory handlerFactory, IQueryHandlerDecoratorFactory decoratorFactory);
    }
}