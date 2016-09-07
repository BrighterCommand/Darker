using System;

namespace Darker
{
    public interface IQueryHandlerDecoratorFactory
    {
        T Create<T>(Type decoratorType) where T : IQueryHandlerDecorator;
    }
}