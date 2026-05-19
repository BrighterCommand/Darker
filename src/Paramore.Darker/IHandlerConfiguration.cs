namespace Paramore.Darker
{
    public interface IHandlerConfiguration
    {
        IQueryHandlerRegistry HandlerRegistry { get; }
        IQueryHandlerFactory HandlerFactory { get; }
        IQueryHandlerDecoratorRegistry DecoratorRegistry { get; }
        IQueryHandlerDecoratorFactory DecoratorFactory { get; }

        IQueryHandlerRegistryAsync HandlerRegistryAsync { get; }
        IQueryHandlerFactoryAsync HandlerFactoryAsync { get; }
        IQueryHandlerDecoratorRegistryAsync DecoratorRegistryAsync { get; }
        IQueryHandlerDecoratorFactoryAsync DecoratorFactoryAsync { get; }
    }
}