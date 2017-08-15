namespace Paramore.Darker
{
    public interface IHandlerConfiguration
    {
        IQueryHandlerRegistry HandlerRegistry { get; }
        IQueryHandlerFactory HandlerFactory { get; }
        IQueryHandlerDecoratorRegistry DecoratorRegistry { get; }
        IQueryHandlerDecoratorFactory DecoratorFactory { get; }
    }
}