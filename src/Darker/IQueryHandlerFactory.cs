using System;

namespace Darker
{
    public interface IQueryHandlerFactory
    {
        T Create<T>(Type handlerType); // where T : IQueryHandler; doesn't work because handlers are resolved as dynamic
    }
}