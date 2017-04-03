namespace Paramore.Darker
{
    public interface IHandlerConfiguration
    {
        IQueryHandlerRegistry HandlerRegistry { get; }
        IQueryHandlerFactory HandlerFactory { get; }
        IQueryHandlerDecoratorFactory DecoratorFactory { get; }
    }
}