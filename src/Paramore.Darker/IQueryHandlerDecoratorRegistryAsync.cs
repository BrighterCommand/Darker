using System;

namespace Paramore.Darker
{
    public interface IQueryHandlerDecoratorRegistryAsync
    {
        void Register(Type decoratorType);
    }
}
