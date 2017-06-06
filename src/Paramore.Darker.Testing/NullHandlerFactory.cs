using System;

namespace Paramore.Darker.Testing
{
    public class NullHandlerFactory : IQueryHandlerFactory
    {
        public IQueryHandler Create(Type handlerType)
        {
            return null;
        }

        public void Release(IQueryHandler handler)
        {
        }
    }
}