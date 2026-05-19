using System;

namespace Paramore.Darker
{
    public interface IQueryHandlerFactoryAsync
    {
        IQueryHandler Create(Type handlerType);
        void Release(IQueryHandler handler);
    }
}
