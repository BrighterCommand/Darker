using System;

namespace Darker
{
    public interface IQueryHandlerFactory
    {
        T Create<T>(Type handlerType);
    }
}