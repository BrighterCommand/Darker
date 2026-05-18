using System;

namespace Paramore.Darker
{
    public interface IQueryHandlerDecoratorFactoryAsync
    {
        T Create<T>(Type decoratorType) where T : IQueryHandlerDecorator;
        void Release<T>(T handler) where T : IQueryHandlerDecorator;
    }
}
