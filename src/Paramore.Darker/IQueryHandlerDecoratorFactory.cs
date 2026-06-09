using System;

namespace Paramore.Darker
{
    public interface IQueryHandlerDecoratorFactory
    {
        T Create<T>(Type decoratorType, IAmALifetime lifetime) where T : IQueryHandlerDecorator;
        void Release<T>(T handler, IAmALifetime lifetime) where T : IQueryHandlerDecorator;
    }
}
