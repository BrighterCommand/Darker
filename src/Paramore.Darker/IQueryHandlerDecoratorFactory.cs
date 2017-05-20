using System;

namespace Paramore.Darker
{
    public interface IQueryHandlerDecoratorFactory
    {
        T Create<T>(Type decoratorType) where T : IQueryHandlerDecorator;
        void Release<T>(T handler) where T : IQueryHandlerDecorator;
    }
}