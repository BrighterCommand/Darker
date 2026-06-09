using System;

namespace Paramore.Darker
{
    public interface IQueryHandlerFactoryAsync
    {
        IQueryHandler Create(Type handlerType, IAmALifetime lifetime);
        void Release(IQueryHandler handler, IAmALifetime lifetime);
    }
}
