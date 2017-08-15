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

        IQueryHandler IQueryHandlerFactory.Create(Type handlerType)
        {
            return (IQueryHandler)_func(handlerType);
        }

        void IQueryHandlerFactory.Release(IQueryHandler handler)
        {
            // no op
        }

        T IQueryHandlerDecoratorFactory.Create<T>(Type handlerType)
        {
            return (T)_func(handlerType);
        }

        void IQueryHandlerDecoratorFactory.Release<T>(T handler)
        {
            // no op
        }
    }
}