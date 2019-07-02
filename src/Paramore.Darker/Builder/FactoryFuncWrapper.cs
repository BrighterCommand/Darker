using System;

namespace Paramore.Darker.Builder
{
    internal sealed class FactoryFuncWrapper : IQueryHandlerFactory, IQueryHandlerDecoratorFactory
    {
        private readonly Func<Type, object> _func;

        public FactoryFuncWrapper(Func<Type, object> func)
        {
            _func = func;
        }

        T IQueryHandlerDecoratorFactory.Create<T>(Type handlerType)
        {
            return (T) _func(handlerType);
        }

        void IQueryHandlerDecoratorFactory.Release<T>(T handler)
        {
            var disposable = handler as IDisposable;
            disposable?.Dispose();
        }

        IQueryHandler IQueryHandlerFactory.Create(Type handlerType)
        {
            return (IQueryHandler) _func(handlerType);
        }

        void IQueryHandlerFactory.Release(IQueryHandler handler)
        {
            var disposable = handler as IDisposable;
            disposable?.Dispose();
        }
    }
}