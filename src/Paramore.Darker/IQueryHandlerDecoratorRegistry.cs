using System;

namespace Paramore.Darker
{
    public interface IQueryHandlerDecoratorRegistry
    {
        void Register(Type decoratorType);
    }
}