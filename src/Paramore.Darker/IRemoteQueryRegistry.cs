using System;

namespace Paramore.Darker
{
    public interface IRemoteQueryRegistry
    {
        bool CanHandle(Type query);
        IQueryHandler ResolveHandler(Type query);
    }
}