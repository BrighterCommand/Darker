using System;

namespace Paramore.Darker.Builder
{
    internal class FactoryFuncWrapper : IQueryHandlerFactory, IQueryHandlerDecoratorFactory
    {
        private readonly Func<Type, object> _func;

        public FactoryFuncWrapper(Func<Type, object> func)
        {
            _func = func;
        }

        T IQueryHandlerDecoratorFactory.Create<T>(Type handlerType)
        {
            return (T)_func(handlerType);
        }

        T IQueryHandlerFactory.Create<T>(Type handlerType)
        {
            return (T)_func(handlerType);
        }
    }
}