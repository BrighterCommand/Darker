using System;

namespace Paramore.Darker.Builder
{
    internal sealed class RegistryActionWrapper : IQueryHandlerDecoratorRegistry
    {
        private readonly Action<Type> _action;

        public RegistryActionWrapper(Action<Type> action)
        {
            _action = action;
        }

        public void Register(Type decoratorType)
        {
            _action(decoratorType);
        }
    }
}