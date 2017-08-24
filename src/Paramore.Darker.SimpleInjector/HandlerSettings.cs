using System.Reflection;

namespace Paramore.Darker.SimpleInjector
{
    public sealed class HandlerSettings
    {
        private readonly SimpleInjectorHandlerRegistry _registry;

        internal HandlerSettings(SimpleInjectorHandlerRegistry registry)
        {
            _registry = registry;
        }

        public HandlerSettings WithQueriesAndHandlersFromAssembly(Assembly assembly)
        {
            _registry.RegisterFromAssemblies(new [] { assembly });
            return this;
        }
    }
}