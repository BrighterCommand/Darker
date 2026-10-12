using System;
using System.Threading.Tasks;

namespace Paramore.Darker
{
    public interface IQueryHandlerFactoryAsync
    {
        IQueryHandler Create(Type handlerType, IAmALifetime lifetime);
        void Release(IQueryHandler handler, IAmALifetime lifetime);
        ValueTask ReleaseAsync(IQueryHandler handler, IAmALifetime lifetime);
    }
}
