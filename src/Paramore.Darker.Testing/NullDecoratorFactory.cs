using System;

namespace Paramore.Darker.Testing
{
    public class NullDecoratorFactory : IQueryHandlerDecoratorFactory
    {
        public T Create<T>(Type decoratorType) where T : IQueryHandlerDecorator
        {
            return default(T);
        }

        public void Release<T>(T handler) where T : IQueryHandlerDecorator
        {
        }
    }
}