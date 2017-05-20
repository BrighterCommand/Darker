using System;

namespace Paramore.Darker
{
    public interface IQueryHandlerFactory
    {
        IQueryHandler Create(Type handlerType);
        void Release(IQueryHandler handler);
    }
}